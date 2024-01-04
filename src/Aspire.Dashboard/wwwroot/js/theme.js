import {
    accentBaseColor,
    baseLayerLuminance,
    SwatchRGB
} from "/_content/Microsoft.FluentUI.AspNetCore.Components/Microsoft.FluentUI.AspNetCore.Components.lib.module.js";

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
export function getSystemTheme() {
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
export function setThemeCookie(theme) {
    document.cookie = `${currentThemeCookieName}=${theme}`;
}

/**
 * Sets the document data-theme attribute to the specified value.
 * @param {string} theme
 */
export function setThemeOnDocument(theme) {

    if (!theme || theme === themeSettingSystem) {
        theme = getSystemTheme();
    }

    if (theme === themeSettingDark) {
        document.documentElement.setAttribute('data-theme', 'dark');
    } else /* Light */ {
        document.documentElement.setAttribute('data-theme', 'light');
    }
}

/**
 * Returns the value of the currentTheme cookie, or System if the cookie is not set.
 * @returns {string}
 */
export function getThemeCookieValue() {
    return getCookieValue(currentThemeCookieName) ?? themeSettingSystem;
}

export function setDefaultBaseLayerLuminance(value) {
    baseLayerLuminance.withDefault(value);
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

function setInitialBaseLayerLuminance() {
    let theme = getThemeCookieValue();

    if (!theme || theme === themeSettingSystem) {
        theme = getSystemTheme();
    }

    if (theme === themeSettingDark) {
        baseLayerLuminance.withDefault(darkThemeLuminance);
    } else /* Light */ {
        baseLayerLuminance.withDefault(lightThemeLuminance);
    }

    setThemeOnDocument(theme);
}

function setInitialAccentColor() {
    // Convert the base color ourselves to avoid pulling in the
    // @microsoft/fast-colors library just for one call to parseColorHexRGB
    const baseColor = { // #512BD4
        r: 0x51 / 255.0,
        g: 0x2B / 255.0,
        b: 0xD4 / 255.0
    };

    const accentBase = SwatchRGB.create(baseColor.r, baseColor.g, baseColor.b);
    accentBaseColor.withDefault(accentBase);
}

setInitialBaseLayerLuminance();
setInitialAccentColor();
