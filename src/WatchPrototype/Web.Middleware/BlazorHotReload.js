// Used by older versions of Microsoft.AspNetCore.Components.WebAssembly.
// For back compat only to support WASM packages older than the SDK.

export function receiveHotReload() {
  return BINDING.js_to_mono_obj(new Promise((resolve) => receiveHotReloadAsync().then(resolve(0))));
}

export async function receiveHotReloadAsync() {
  const response = await fetch('/_framework/blazor-hotreload');
  if (response.status === 200) {
    const updates = await response.json();
    if (updates) {
      try {
        updates.forEach(u => {
          u.deltas.forEach(d => window.Blazor._internal.applyHotReload(d.moduleId, d.metadataDelta, d.ilDelta, d.pdbDelta, d.updatedTypes));
        })
      } catch (error) {
        console.warn(error);
        return;
      }
    }
  }
}
