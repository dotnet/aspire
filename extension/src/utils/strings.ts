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