import { sendToAspireTerminal } from '../utils/terminal';

export async function newCommand() {
    sendToAspireTerminal("aspire new");
};
