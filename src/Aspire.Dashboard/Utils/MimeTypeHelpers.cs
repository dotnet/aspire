// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Utils;

public static class MimeTypeHelpers
{
    // Browsers support a limited set of image mime types.
    // See: https://developer.mozilla.org/en-US/docs/Web/Media/Formats/Image_types
    // We only allow these known safe types to avoid broken image in the browser.
    public static readonly HashSet<string> SupportedImageTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/apng",
        "image/avif",
        "image/bmp",
        "image/gif",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/svg+xml",
        "image/webp",
        "image/x-icon"
    };

    public static readonly HashSet<string> SupportedAudioTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",
        "audio/wav",
        "audio/x-wav",
        "audio/mp4",
        "audio/aac",
        "audio/ogg",
        "audio/flac",
        "audio/webm"
    };

    public static readonly HashSet<string> SupportedVideoTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/webm",
        "video/ogg",
        "video/webm"
    };

    // AI generated list of extensions.
    // Combines 100 commonly used mime types with https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types.
    public static readonly Dictionary<string, string> MimeToExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Audio (your required + MDN additions)
        { "audio/mpeg", ".mp3" },
        { "audio/wav", ".wav" },
        { "audio/x-wav", ".wav" },
        { "audio/mp4", ".m4a" },
        { "audio/aac", ".aac" },
        { "audio/ogg", ".ogg" },
        { "audio/flac", ".flac" },
        { "audio/webm", ".weba" },
        { "audio/midi", ".mid" },
        { "audio/x-midi", ".midi" },
        { "audio/opus", ".opus" },

        // Image (your required + MDN additions)
        { "image/apng", ".apng" },
        { "image/avif", ".avif" },
        { "image/bmp", ".bmp" },
        { "image/gif", ".gif" },
        { "image/jpeg", ".jpeg" },
        { "image/jpg", ".jpg" },
        { "image/png", ".png" },
        { "image/svg+xml", ".svg" },
        { "image/webp", ".webp" },
        { "image/x-icon", ".ico" },
        { "image/vnd.microsoft.icon", ".ico" },
        { "image/heif", ".heif" },
        { "image/heic", ".heic" },

        // Video
        { "video/mp4", ".mp4" },
        { "video/webm", ".webm" },
        { "video/ogg", ".ogv" },
        { "video/quicktime", ".mov" },
        { "video/x-msvideo", ".avi" },
        { "video/x-ms-wmv", ".wmv" },
        { "video/mpeg", ".mpeg" },
        { "video/x-flv", ".flv" },
        { "video/3gpp", ".3gp" },
        { "video/3gpp2", ".3g2" },

        // Text / Markup
        { "text/plain", ".txt" },
        { "text/html", ".html" },
        { "text/css", ".css" },
        { "text/javascript", ".js" },
        { "application/javascript", ".js" },
        { "application/ld+json", ".jsonld" },
        { "application/vnd.api+json", ".json" },
        { "application/json", ".json" },
        { "application/xml", ".xml" },
        { "text/xml", ".xml" },
        { "text/csv", ".csv" },
        { "text/markdown", ".md" },
        { "text/calendar", ".ics" },
        { "application/xhtml+xml", ".xhtml" },

        // Applications / Documents
        { "application/pdf", ".pdf" },
        { "application/msword", ".doc" },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
        { "application/vnd.ms-excel", ".xls" },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" },
        { "application/vnd.ms-powerpoint", ".ppt" },
        { "application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx" },
        { "application/rtf", ".rtf" },
        { "application/epub+zip", ".epub" },
        { "application/vnd.oasis.opendocument.text", ".odt" },
        { "application/vnd.oasis.opendocument.spreadsheet", ".ods" },
        { "application/vnd.oasis.opendocument.presentation", ".odp" },
        { "application/vnd.oasis.opendocument.graphics", ".odg" },
        { "application/vnd.oasis.opendocument.chart", ".odc" },
        { "application/vnd.oasis.opendocument.image", ".odi" },
        { "application/vnd.oasis.opendocument.formula", ".odf" },
        { "application/vnd.oasis.opendocument.database", ".odb" },
        { "application/vnd.visio", ".vsd" },
        { "application/vnd.google-earth.kml+xml", ".kml" },
        { "application/vnd.google-earth.kmz", ".kmz" },
        { "application/vnd.amazon.ebook", ".azw" },
        { "application/vnd.apple.installer+xml", ".mpkg" },

        // Archives / Compressed
        { "application/zip", ".zip" },
        { "application/x-rar-compressed", ".rar" },
        { "application/gzip", ".gz" },
        { "application/x-7z-compressed", ".7z" },
        { "application/x-tar", ".tar" },
        { "application/x-bzip2", ".bz2" },
        { "application/x-bzip", ".bz" },
        { "application/x-freearc", ".arc" },
        { "application/x-abiword", ".abw" },

        // Fonts
        { "application/font-woff", ".woff" },
        { "application/font-woff2", ".woff2" },
        { "application/vnd.ms-fontobject", ".eot" },
        { "application/x-font-ttf", ".ttf" },
        { "application/x-font-opentype", ".otf" },

        // Binary / Executables
        { "application/octet-stream", ".bin" },
        { "application/x-dosexec", ".exe" },
        { "application/java-archive", ".jar" },
        { "application/vnd.android.package-archive", ".apk" },
        { "application/x-httpd-php", ".php" },

        // Scripts
        { "application/x-sh", ".sh" },
        { "application/x-perl", ".pl" },
        { "application/x-ruby", ".rb" },
        { "application/x-python-code", ".pyc" },
        { "application/x-lua-bytecode", ".luac" },

        // Miscellaneous
        { "multipart/form-data", ".form" },
        { "application/x-www-form-urlencoded", ".urlencoded" },
        { "application/x-iso9660-image", ".iso" },
        { "application/x-bittorrent", ".torrent" },
        { "application/x-shockwave-flash", ".swf" },
        { "application/vnd.mozilla.xul+xml", ".xul" }
    };
}
