window.farmCulture = {
    get: function () {
        return window.localStorage.getItem('farm-language') || 'en';
    },
    set: function (code) {
        window.localStorage.setItem('farm-language', code);
        document.documentElement.lang = code;
    }
};
