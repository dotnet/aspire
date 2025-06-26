const emojiMap: { [key: string]: string } = {
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

export function formatText(str: string): string {
  return str.replace(/:[a-z]+(?:_[a-z]+)*:/g, match => emojiMap[match] || match);
}