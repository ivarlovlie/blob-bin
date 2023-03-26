function blobToBase64(blob) {
    return new Promise((resolve, _) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.readAsDataURL(blob);
    });
}

async function arrayBufferToBase64(data) {
    // Use a FileReader to generate a base64 data URI
    const base64url = await new Promise((r) => {
        const reader = new FileReader()
        reader.onload = () => r(reader.result)
        reader.readAsDataURL(new Blob([data]))
    })

    /*
    The result looks like 
    "data:application/octet-stream;base64,<your base64 data>", 
    so we split off the beginning:
    */
    return base64url.split(",", 2)[1]
}

class VeryEncrypted {
    key
    #iterations = 10000
    #keyBytesIndex = 32
    #contentIndex = 16
    #saltIndex = 8
    #keyBytes
    #TextEncoder

    constructor(key) {
        this.key = key;
        this.#TextEncoder = new TextEncoder();
        this.#keyBytes = this.#TextEncoder.encode(this.key);
    }

    async #getKey() {
        return await crypto.subtle.importKey("raw", this.#keyBytes, {name: "PBKDF2"}, false, ["deriveBits"])
    }

    async #getPBKDF2Bytes(salt) {
        return new Uint8Array(await crypto.subtle.deriveBits({
            name: 'PBKDF2',
            salt: salt,
            iterations: this.#iterations,
            hash: 'SHA-256'
        }, await this.#getKey(), 384))
    }

    async decrypt(data) {
        const bytes = new Uint8Array(data)
        const salt = bytes.slice(this.#saltIndex, this.#contentIndex)
        const pbkdf2Bytes = await this.#getPBKDF2Bytes(salt);
        const keyBytes = pbkdf2Bytes.slice(0, this.#keyBytesIndex)
        const ivBytes = pbkdf2Bytes.slice(this.#keyBytesIndex)
        const decryptionKey = await window.crypto.subtle.importKey('raw', keyBytes, {
            name: 'AES-CBC',
            length: 256
        }, false, ['decrypt']);
        const ww = bytes.slice(this.#contentIndex);
        const decryptedBytes = await window.crypto.subtle.decrypt({
            name: "AES-CBC",
            iv: ivBytes
        }, decryptionKey, ww)
        console.log(decryptedBytes)
        if (!decryptedBytes) {
            alert("Decrypt operation failed")
            return
        }
        return new Uint8Array(decryptedBytes);
    }

    async encrypt(data) {
        const salt = crypto.getRandomValues(new Uint8Array(8));
        const bytes = await this.#getPBKDF2Bytes()
        const keyBytes = bytes.slice(0, this.#keyBytesIndex)
        const ivBytes = bytes.slice(this.#keyBytesIndex)
        const encryptionKey = await crypto.subtle.importKey('raw', keyBytes, {
            name: 'AES-CBC',
            length: 256
        }, false, ['encrypt'])
        const encryptedBytes = new Uint8Array(await crypto.subtle.encrypt({
            name: "AES-CBC",
            iv: ivBytes
        }, encryptionKey, data))
        if (!encryptedBytes) {
            alert("Encrypt operation failed")
            return
        }
        const result = new Uint8Array(encryptedBytes.length + this.#contentIndex)
        result.set(this.#TextEncoder.encode('Salted__'))
        result.set(salt, this.#saltIndex)
        result.set(encryptedBytes, this.#contentIndex)
        return result
    }
}
