declare module 'asciinema-player' {
  export interface AsciinemaPlayerOptions {
    autoplay?: boolean;
    loop?: boolean;
    poster?: string;
    fit?: 'width' | 'height' | 'both' | 'none';
    speed?: number;
    theme?: string;
    preload?: boolean;
    startAt?: number | string;
    idleTimeLimit?: number;
    terminalFontSize?: string;
    terminalLineHeight?: number;
    terminalFontFamily?: string;
    rows?: number | undefined;
  }

  export function create(
    src: string,
    element: HTMLElement,
    options?: AsciinemaPlayerOptions
  ): Promise<any>;
}
