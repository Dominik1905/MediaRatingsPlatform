window.mrpAuth = {
    set: function (username, token) {
        localStorage.setItem("mrp_username", username || "");
        localStorage.setItem("mrp_token", token || "");
    },
    clear: function () {
        localStorage.removeItem("mrp_username");
        localStorage.removeItem("mrp_token");
    },
    getUsername: function () {
        return localStorage.getItem("mrp_username") || "";
    },
    getToken: function () {
        return localStorage.getItem("mrp_token") || "";
    }
};
