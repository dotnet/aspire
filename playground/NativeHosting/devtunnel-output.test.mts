import assert from 'node:assert/strict';
import { test } from 'node:test';
import { TunnelOutputParser } from './devtunnel-output.mts';

test('many short lines may arrive in one chunk larger than the line limit', () => {
    const parser = new TunnelOutputParser('abc', 7007);
    assert.deepEqual(parser.parse('ignored\n'.repeat(4096)), Array.from({ length: 4096 }, () => ({})));
});

test('the line limit applies to complete and partial lines across chunks', () => {
    const parser = new TunnelOutputParser('abc', 7007);
    assert.deepEqual(parser.parse('x'.repeat(16384)), []);
    assert.deepEqual(parser.parse('\n'), [{}]);
    for (const suffix of ['', '\n']) {
        assert.throws(() => new TunnelOutputParser('abc', 7007).parse('x'.repeat(16385) + suffix), /bounded/);
    }
    const split = new TunnelOutputParser('abc', 7007);
    split.parse('x'.repeat(16384));
    assert.throws(() => split.parse('x\n'), /bounded/);
});

test('a public endpoint may span chunks after a CRLF-terminated hosting line', () => {
    const parser = new TunnelOutputParser('abc', 7007);
    assert.deepEqual(parser.parse('Hosting port: 7007\r\nConnect via browser: https://abc-'), [{}]);
    assert.deepEqual(parser.parse('7007.usw2.devtunnels.ms/\n'), [{ url: 'https://abc-7007.usw2.devtunnels.ms/' }]);
});
