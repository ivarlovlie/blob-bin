document.addEventListener("DOMContentLoaded", () => {
    const decryptkey = document.getElementById("decrypt-key")
    const btn = document.getElementById("btn-decrypt")
    btn.addEventListener("click", async () => {
        if (decryptkey.value) {
            const venc = new VeryEncrypted(decryptkey.value);
            const encryptedContent = document.getElementById("content");
            const decryptedContentString = await venc.decrypt(new TextEncoder().encode(atob(encryptedContent.innerText)));
            const decryptedContent = document.createElement("pre");
            decryptedContent.innerText = new TextDecoder().decode(decryptedContentString);
            document.body.appendChild(decryptedContent)
        }
    })
})