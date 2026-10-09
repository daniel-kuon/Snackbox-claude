// Global barcode scanner handler
window.barcodeScanner = (function () {
    // Keyed by an id the component passes in. Each interop call hands JS a new proxy for the
    // same .NET object, so looking a registration up by the object never found it and
    // unregister silently did nothing - registrations piled up across pages.
    const registeredComponents = new Map();
    let buffer = '';
    let lastKeyTime = 0;
    const KEY_TIMEOUT = 50; // milliseconds between keystrokes for barcode scanner
    const MIN_LENGTH = 3; // minimum barcode length
    const COOLDOWN = 500; // cooldown period after scan

    let isInCooldown = false;

    function isEditable(element) {
        return !!element && (element.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(element.tagName));
    }

    function handleKeyPress(e) {
        // A focused field owns its keystrokes - the scan goes into it the normal way. This
        // listener is for scans while no field has focus; it used to grab them everywhere and
        // swallow the Enter of fields that take scans themselves (the card wizard).
        if (isEditable(e.target)) {
            buffer = '';
            return;
        }

        const currentTime = Date.now();
        const timeDiff = currentTime - lastKeyTime;

        // Enter key signals end of barcode - only taken when a BarcodeInput is listening
        if (e.key === 'Enter' && buffer.length >= MIN_LENGTH && registeredComponents.size > 0) {
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
        register: function (key, component) {
            registeredComponents.set(key, component);
        },
        unregister: function (key) {
            registeredComponents.delete(key);
        }
    };
})();
