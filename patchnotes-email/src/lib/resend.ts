import { Resend } from "resend";

const apiKey = process.env.RESEND_API_KEY;
if (!apiKey) {
    throw new Error("RESEND_API_KEY environment variable is required");
}

export const resend = new Resend(apiKey);

export const FROM_ADDRESS = "PatchNotes <notifications@patchnotes.dev>";

export function escapeHtml(str: string): string {
    return str
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}
