const currentThemeCookieName = "currentTheme";
const themeSettingSystem = "System";
const themeSettingDark = "Dark";
const themeSettingLight = "Light";
const darkThemeLuminance = 0.15;
const lightThemeLuminance = 0.95;

/**
 * Returns the current system theme (Light or Dark)
 * @returns {string}
 */
window.getSystemTheme = function () {
    let matched = window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (matched) {
        return themeSettingDark;
    } else {
        return themeSettingLight;
    }
}

/**
 * Sets the currentTheme cookie to the specified value.
 * @param {string} theme
 */
window.setThemeCookie = function (theme) {
    document.cookie = `${currentThemeCookieName}=${theme}`;
}

/**
 * Returns the value of the currentTheme cookie, or System if the cookie is not set.
 * @returns {string}
 */
window.getThemeCookieValue = function () {
    return getCookieValue(currentThemeCookieName) ?? themeSettingSystem;
}

/**
 * Returns the value of the specified cookie, or the empty string if the cookie is not present
 * @param {string} cookieName
 * @returns {string}
 */
function getCookieValue(cookieName) {
    const cookiePieces = document.cookie.split(';');
    for (let index = 0; index < cookiePieces.length; index++) {
        if (cookiePieces[index].trim().startsWith(cookieName)) {
            const cookieKeyValue = cookiePieces[index].split('=');
            if (cookieKeyValue.length > 1) {
                return cookieKeyValue[1];
            }
        }
    }

    return "";
}

let theme = window.getThemeCookieValue();

if (!theme || theme === themeSettingSystem) {
    theme = window.getSystemTheme();
}

if (theme === themeSettingDark) {
    window.DefaultBaseLayerLuminance = darkThemeLuminance;
} else /* Light */ {
    window.DefaultBaseLayerLuminance = lightThemeLuminance;
}

window.DefaultBaseAccentColor = "#512BD4";
