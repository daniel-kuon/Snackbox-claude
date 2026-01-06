// Global barcode scanner handler
window.barcodeScanner = (function () {
    const registeredComponents = [];
    let buffer = '';
    let lastKeyTime = 0;
    const KEY_TIMEOUT = 50; // milliseconds between keystrokes for barcode scanner
    const MIN_LENGTH = 3; // minimum barcode length
    const COOLDOWN = 500; // cooldown period after scan

    let isInCooldown = false;

    function handleKeyPress(e) {
        const currentTime = Date.now();
        const timeDiff = currentTime - lastKeyTime;

        // Enter key signals end of barcode
        if (e.key === 'Enter' && buffer.length >= MIN_LENGTH) {
            e.preventDefault();
            
            if (!isInCooldown) {
                broadcastBarcode(buffer);
                isInCooldown = true;
                setTimeout(() => { isInCooldown = false; }, COOLDOWN);
            }
            
            buffer = '';
            lastKeyTime = 0;
            return;
        }

        // If keys are pressed rapidly (like from a scanner)
        if (timeDiff < KEY_TIMEOUT || buffer.length === 0) {
            // Single character keys only
            if (e.key.length === 1) {
                buffer += e.key;
                lastKeyTime = currentTime;
            }
        } else {
            // Too slow, reset buffer (human typing)
            buffer = e.key.length === 1 ? e.key : '';
            lastKeyTime = currentTime;
        }

        // Auto-reset buffer after timeout
        setTimeout(() => {
            if (Date.now() - lastKeyTime > 200) {
                buffer = '';
            }
        }, 250);
    }

    function broadcastBarcode(barcode) {
        console.log('Barcode scanned:', barcode);
        
        registeredComponents.forEach(component => {
            try {
                component.invokeMethodAsync('OnBarcodeScanned', barcode);
            } catch (error) {
                console.error('Error broadcasting barcode:', error);
            }
        });
    }

    // Initialize event listener
    document.addEventListener('keypress', handleKeyPress);

    return {
        register: function (component) {
            if (!registeredComponents.includes(component)) {
                registeredComponents.push(component);
                console.log('Registered barcode component, total:', registeredComponents.length);
            }
        },
        unregister: function (component) {
            const index = registeredComponents.indexOf(component);
            if (index > -1) {
                registeredComponents.splice(index, 1);
                console.log('Unregistered barcode component, remaining:', registeredComponents.length);
            }
        }
    };
})();
