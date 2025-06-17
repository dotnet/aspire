/**
 * This module provides functions to help with image importing in Astro.
 * It solves the problem of dynamic imports from JSON data.
 */

// Import all images from the testimonials directory
// This uses Vite's import.meta.glob feature
const testimonialImages = import.meta.glob('../assets/testimonials/*.{png,jpg,jpeg,webp}', { eager: true });

/**
 * Get an imported image by path
 * @param {string} path - The path to the image (e.g., "../assets/testimonials/image.png")
 * @returns {string} The processed image URL or the original path if not found
 */
export function getImageByPath(path) {
    if (!path || typeof path !== 'string' || !path.startsWith('../assets/')) {
        return path;
    }
    
    const importedImage = testimonialImages[path];
    
    return importedImage?.default || path;
}

/**
 * Process an array of objects with avatar properties to use imported images
 * @param {Array} items - Array of objects containing avatar properties
 * @returns {Array} The processed array with imported images
 */
export function processAvatars(items) {
    if (!Array.isArray(items)) return [];
    
    return items.map(item => {
        if (!item || typeof item !== 'object') return item;
        
        return {
            ...item,
            avatar: getImageByPath(item.avatar)
        };
    });
}
