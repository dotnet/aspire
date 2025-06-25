import * as assert from 'assert';
import { formatText } from '../utils/strings';

suite('utils/strings tests', () => {
	test('formatText formats correctly ', () => {
        const input = 'This is a test :ice: :rocket: :bug: :microscope: :linked_paperclips: :chart_increasing: :chart_decreasing: :locked_with_key: :play_button: :check_mark: :cross_mark: :hammer_and_wrench:';
        const expectedOutput = 'This is a test 🧊 🚀 🐛 🔬 🔗 📈 📉 🔒 ▶️ ✅ ❌ 🛠️';
        const result = formatText(input);
        assert.strictEqual(result, expectedOutput);

        const inputWithUnknownEmoji = 'This is a test :unknown_emoji:';
        const expectedOutputWithUnknownEmoji = 'This is a test :unknown_emoji:';
        const resultWithUnknownEmoji = formatText(inputWithUnknownEmoji);
        assert.strictEqual(resultWithUnknownEmoji, expectedOutputWithUnknownEmoji);

        const inputWithNoEmojis = 'This is a test without emojis.';
        const expectedOutputWithNoEmojis = 'This is a test without emojis.';
        const resultWithNoEmojis = formatText(inputWithNoEmojis);
        assert.strictEqual(resultWithNoEmojis, expectedOutputWithNoEmojis);
	});
});