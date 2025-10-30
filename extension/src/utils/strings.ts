const emojiMap: { [key: string]: string; } = {
  ':ice:': '🧊',
  ':rocket:': '🚀',
  ':bug:': '🐛',
  ':microscope:': '🔬',
  ':linked_paperclips:': '🔗',
  ':chart_increasing:': '📈',
  ':chart_decreasing:': '📉',
  ':locked_with_key:': '🔒',
  ':play_button:': '▶️',
  ':check_mark:': '✅',
  ':cross_mark:': '❌',
  ':hammer_and_wrench:': '🛠️'
};

/**
 * Formats a string by replacing emoji codes (such as :ice:) with their corresponding Unicode characters.
 */
export function formatText(str: string): string {
  return str.replace(/:[a-z]+(?:_[a-z]+)*:/g, match => emojiMap[match] || match);
}

export function removeTrailingNewline(str: string): string {
  return str.replace(/(\r\n|\n)$/, '');
}

export function applyTextStyle(text: string, style: string | null | undefined): string {
  if (!style) {
    return text;
  }

  return `${style}${text}\x1b[0m`;
}