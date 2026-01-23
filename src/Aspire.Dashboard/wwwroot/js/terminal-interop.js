// Terminal interop for xterm.js integration with Aspire Dashboard
// Uses the Aspire Terminal Protocol (ATP) - JSON messages over WebSocket

const terminals = new Map();

// Properly decode base64 to UTF-8 string
function decodeBase64Utf8(base64) {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return new TextDecoder('utf-8').decode(bytes);
}

// Properly encode UTF-8 string to base64
function encodeUtf8Base64(str) {
    const bytes = new TextEncoder().encode(str);
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
}

// Initialize a terminal in the specified container
export async function initTerminal(containerId, wsUrl, dotnetRef) {
    // Wait for xterm scripts to load
    await loadXtermScripts();

    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Terminal container not found:', containerId);
        return false;
    }

    // Clean up any existing terminal in this container
    if (terminals.has(containerId)) {
        disposeTerminal(containerId);
    }

    const terminal = new Terminal({
        cursorBlink: true,
        fontSize: 14,
        fontFamily: '"Cascadia Code", "Cascadia Mono", "Fira Code", "JetBrains Mono", Menlo, Monaco, Consolas, monospace',
        lineHeight: 1.1,
        theme: {
            background: '#1e1e1e',
            foreground: '#d4d4d4',
            cursor: '#d4d4d4',
            cursorAccent: '#1e1e1e',
            selectionBackground: 'rgba(255, 255, 255, 0.3)'
        },
        allowProposedApi: true
    });

    // Load addons
    const fitAddon = new FitAddon.FitAddon();
    const webLinksAddon = new WebLinksAddon.WebLinksAddon();
    const unicode11Addon = new Unicode11Addon.Unicode11Addon();

    terminal.loadAddon(fitAddon);
    terminal.loadAddon(webLinksAddon);
    terminal.loadAddon(unicode11Addon);
    terminal.unicode.activeVersion = '11';

    terminal.open(container);
    fitAddon.fit();

    // Store terminal state
    const state = {
        terminal,
        fitAddon,
        webLinksAddon,
        unicode11Addon,
        ws: null,
        wsUrl,
        dotnetRef,
        reconnecting: false,
        disposed: false
    };
    terminals.set(containerId, state);

    // Handle terminal resize
    terminal.onResize(({ cols, rows }) => {
        sendResize(state, cols, rows);
    });

    // Handle terminal input (keyboard)
    terminal.onData((data) => {
        sendInput(state, data);
    });

    // Handle binary input (mouse events)
    terminal.onBinary((data) => {
        sendBinary(state, data);
    });

    // Start WebSocket connection
    connect(state, containerId);

    return true;
}

// Connect to the WebSocket server
function connect(state, containerId) {
    if (state.disposed) return;
    if (!state.wsUrl) {
        updateStatus(state, 'no-url');
        return;
    }

    console.log('Terminal connecting to:', state.wsUrl);
    
    try {
        state.ws = new WebSocket(state.wsUrl);
    } catch (e) {
        console.error('Failed to create WebSocket:', e);
        updateStatus(state, 'error');
        return;
    }

    state.ws.onopen = () => {
        console.log('Terminal WebSocket connected');
        updateStatus(state, 'connected');
        sendResize(state, state.terminal.cols, state.terminal.rows);
        state.terminal.focus();
    };

    state.ws.onclose = (event) => {
        console.log('Terminal WebSocket closed:', event.code, event.reason);
        updateStatus(state, 'disconnected');
        if (!state.disposed) {
            // Auto-reconnect after delay
            state.reconnecting = true;
            setTimeout(() => {
                if (!state.disposed) {
                    connect(state, containerId);
                }
            }, 2000);
        }
    };

    state.ws.onerror = (err) => {
        console.error('Terminal WebSocket error:', err);
        // The error event doesn't have useful info, the close event will follow
    };

    state.ws.onmessage = (event) => {
        try {
            const msg = JSON.parse(event.data);
            if (msg.type === 'output' && msg.data) {
                const text = decodeBase64Utf8(msg.data);
                state.terminal.write(text);
            } else if (msg.type === 'state' && msg.data) {
                state.terminal.clear();
                const text = decodeBase64Utf8(msg.data);
                state.terminal.write(text);
            }
        } catch (e) {
            // Raw output (legacy support)
            state.terminal.write(event.data);
        }
    };
}

function sendResize(state, cols, rows) {
    if (state.ws && state.ws.readyState === WebSocket.OPEN) {
        state.ws.send(JSON.stringify({
            type: 'resize',
            cols: cols,
            rows: rows
        }));
    }
}

function sendInput(state, data) {
    if (state.ws && state.ws.readyState === WebSocket.OPEN) {
        state.ws.send(JSON.stringify({
            type: 'input',
            data: encodeUtf8Base64(data)
        }));
    }
}

function sendBinary(state, data) {
    if (state.ws && state.ws.readyState === WebSocket.OPEN) {
        // Binary data is already byte-string, use btoa directly
        state.ws.send(JSON.stringify({
            type: 'input',
            data: btoa(data)
        }));
    }
}

function updateStatus(state, status) {
    if (state.dotnetRef) {
        state.dotnetRef.invokeMethodAsync('UpdateStatus', status);
    }
}

// Resize the terminal to fit its container
export function fitTerminal(containerId) {
    const state = terminals.get(containerId);
    if (state) {
        state.fitAddon.fit();
    }
}

// Focus the terminal
export function focusTerminal(containerId) {
    const state = terminals.get(containerId);
    if (state) {
        state.terminal.focus();
    }
}

// Update the WebSocket URL (for reconnecting to a restarted resource)
export function updateTerminalUrl(containerId, wsUrl) {
    const state = terminals.get(containerId);
    if (state) {
        state.wsUrl = wsUrl;
        // If not connected, try to connect
        if (!state.ws || state.ws.readyState !== WebSocket.OPEN) {
            connect(state, containerId);
        }
    }
}

// Dispose of the terminal
export function disposeTerminal(containerId) {
    const state = terminals.get(containerId);
    if (state) {
        state.disposed = true;
        if (state.ws) {
            state.ws.close();
        }
        state.terminal.dispose();
        terminals.delete(containerId);
    }
}

// Load xterm.js scripts dynamically
let xtermLoaded = false;
let xtermLoadPromise = null;

async function loadXtermScripts() {
    if (xtermLoaded) return;
    if (xtermLoadPromise) return xtermLoadPromise;

    xtermLoadPromise = new Promise((resolve, reject) => {
        // Use local bundled files instead of CDN
        const scripts = [
            '/js/xterm-5.5.0.min.js',
            '/js/xterm-addon-fit-0.10.0.min.js',
            '/js/xterm-addon-web-links-0.11.0.min.js',
            '/js/xterm-addon-unicode11-0.8.0.min.js'
        ];

        const css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = '/css/xterm-5.5.0.min.css';
        document.head.appendChild(css);

        let loaded = 0;
        const total = scripts.length;

        function loadNext(index) {
            if (index >= scripts.length) {
                xtermLoaded = true;
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = scripts[index];
            script.onload = () => {
                loaded++;
                loadNext(index + 1);
            };
            script.onerror = () => reject(new Error('Failed to load xterm script: ' + scripts[index]));
            document.head.appendChild(script);
        }

        loadNext(0);
    });

    return xtermLoadPromise;
}

// Handle window resize for all terminals
window.addEventListener('resize', () => {
    for (const [containerId, state] of terminals) {
        if (state.fitAddon) {
            state.fitAddon.fit();
        }
    }
});
