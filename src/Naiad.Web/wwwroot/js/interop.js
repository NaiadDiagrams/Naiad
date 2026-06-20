window.statePreference = {
    get: function (key) {
        return localStorage.getItem(key);
    },
    set: function (key, value) {
        localStorage.setItem(key, value);
    },
    remove: function (key) {
        localStorage.removeItem(key);
    }
};

window.fileDownload = {
    downloadBlob: function (filename, contentType, base64Content) {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};

window.appInfo = {
    userAgent: function () {
        return navigator.userAgent;
    }
};

window.themeManager = {
    applyTheme: function (themeName) {
        document.documentElement.setAttribute('data-theme', themeName.toLowerCase());
    },
    initializeTheme: function () {
        const savedTheme = localStorage.getItem('selectedTheme');
        if (savedTheme) {
            document.documentElement.setAttribute('data-theme', savedTheme.toLowerCase());
        }
    }
};

window.diagramExport = {
    // Rasterizes a self-contained SVG (native <text>, no foreignObject/@import) to a PNG, returned as a
    // bare base64 string for fileDownload.downloadBlob. The SVG's pixel size is taken from its viewBox —
    // Naiad emits width="100%" with no height, which gives an <img> no intrinsic size, so explicit
    // width/height are stamped on before serialising. scale multiplies that size for crisper output.
    svgToPng: async function (svgMarkup, scale) {
        const parsed = new DOMParser().parseFromString(svgMarkup, 'image/svg+xml');
        const svg = parsed.documentElement;

        let width = parseFloat(svg.getAttribute('width'));
        let height = parseFloat(svg.getAttribute('height'));
        const viewBox = svg.getAttribute('viewBox');
        if ((!width || !height) && viewBox) {
            const parts = viewBox.split(/[\s,]+/).map(Number);
            width = parts[2];
            height = parts[3];
        }
        width = Math.max(1, Math.round(width || 0));
        height = Math.max(1, Math.round(height || 0));

        svg.setAttribute('width', width);
        svg.setAttribute('height', height);
        const serialized = new XMLSerializer().serializeToString(svg);

        const url = URL.createObjectURL(new Blob([serialized], { type: 'image/svg+xml;charset=utf-8' }));
        try {
            const image = new Image();
            image.width = width;
            image.height = height;
            await new Promise((resolve, reject) => {
                image.onload = () => resolve();
                image.onerror = () => reject(new Error('Could not rasterize the diagram'));
                image.src = url;
            });

            const targetWidth = Math.max(1, Math.round(width * scale));
            const targetHeight = Math.max(1, Math.round(height * scale));
            const canvas = document.createElement('canvas');
            canvas.width = targetWidth;
            canvas.height = targetHeight;

            const context = canvas.getContext('2d');
            context.fillStyle = '#ffffff';
            context.fillRect(0, 0, targetWidth, targetHeight);
            context.drawImage(image, 0, 0, targetWidth, targetHeight);

            const dataUrl = canvas.toDataURL('image/png');
            return dataUrl.substring(dataUrl.indexOf(',') + 1);
        } finally {
            URL.revokeObjectURL(url);
        }
    }
};
