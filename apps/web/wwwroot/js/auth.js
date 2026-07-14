window.farmAuth = {
    get: function () {
        return window.localStorage.getItem('farm-token');
    },
    set: function (token) {
        window.localStorage.setItem('farm-token', token);
    },
    clear: function () {
        window.localStorage.removeItem('farm-token');
    }
};
