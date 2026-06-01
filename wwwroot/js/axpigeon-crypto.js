"use strict";

const AxpigeonCrypto = (function () {

    const PBKDF2_ITERATIONS = 100000;

    function hexToBytes(hex) {
        const bytes = new Uint8Array(hex.length / 2);
        for (let i = 0; i < bytes.length; i++) {
            bytes[i] = parseInt(hex.substr(i * 2, 2), 16);
        }
        return bytes;
    }

    async function deriveKek(passphrase, saltHex) {
        const enc = new TextEncoder();
        const salt = hexToBytes(saltHex);

        const keyMaterial = await crypto.subtle.importKey(
            "raw", enc.encode(passphrase), "PBKDF2", false, ["deriveKey"]
        );

        return crypto.subtle.deriveKey(
            { name: "PBKDF2", salt: salt, iterations: PBKDF2_ITERATIONS, hash: "SHA-256" },
            keyMaterial,
            { name: "AES-GCM", length: 256 },
            false,
            ["decrypt"]
        );
    }

    async function unwrapDek(kek, wrappedDekB64, ivHex) {
        const iv = hexToBytes(ivHex);
        const wrappedBytes = Uint8Array.from(atob(wrappedDekB64), c => c.charCodeAt(0));

        const plainBytes = await crypto.subtle.decrypt(
            { name: "AES-GCM", iv: iv },
            kek,
            wrappedBytes
        );

        return new TextDecoder().decode(plainBytes);
    }

    function formatKeyBytes(secretKey) {
        const enc = new TextEncoder();
        const keyBytes = enc.encode(secretKey);
        const formatted = new Uint8Array(32);
        formatted.set(keyBytes.slice(0, Math.min(keyBytes.length, 32)));
        return formatted;
    }

    function formatIvBytes(secretKey) {
        const enc = new TextEncoder();
        const ivBytes = enc.encode(secretKey);
        const formatted = new Uint8Array(16);
        formatted.set(ivBytes.slice(0, Math.min(ivBytes.length, 16)));
        return formatted;
    }

    async function decryptMessageCBC(secretKey, base64Message) {
        const keyBytes = formatKeyBytes(secretKey);
        const ivBytes = formatIvBytes(secretKey);

        const key = await crypto.subtle.importKey(
            "raw", keyBytes, { name: "AES-CBC" }, false, ["decrypt"]
        );

        const encryptedBytes = Uint8Array.from(atob(base64Message), c => c.charCodeAt(0));

        const decrypted = await crypto.subtle.decrypt(
            { name: "AES-CBC", iv: ivBytes },
            key,
            encryptedBytes
        );

        return new TextDecoder().decode(decrypted);
    }

    async function decryptTransaction(passphrase, wrappedDek, dekSalt, dekIv, encryptedMessage) {
        const kek = await deriveKek(passphrase, dekSalt);
        const dek = await unwrapDek(kek, wrappedDek, dekIv);
        return await decryptMessageCBC(dek, encryptedMessage);
    }

    let _cachedKek = null;
    let _cachedSalt = null;

    function setCachedKek(kek, salt) {
        _cachedKek = kek;
        _cachedSalt = salt;
    }

    function getCachedKek(salt) {
        if (_cachedKek && _cachedSalt === salt) return _cachedKek;
        return null;
    }

    function clearCache() {
        _cachedKek = null;
        _cachedSalt = null;
    }

    async function decryptWithCache(passphrase, wrappedDek, dekSalt, dekIv, encryptedMessage) {
        let kek = getCachedKek(dekSalt);
        if (!kek) {
            kek = await deriveKek(passphrase, dekSalt);
            setCachedKek(kek, dekSalt);
        }
        const dek = await unwrapDek(kek, wrappedDek, dekIv);
        return await decryptMessageCBC(dek, encryptedMessage);
    }

    return {
        deriveKek,
        unwrapDek,
        decryptMessageCBC,
        decryptTransaction,
        decryptWithCache,
        clearCache
    };
})();
