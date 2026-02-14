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

export const SETTINGS_URL = "https://myreleasenotes.ai/settings";

export function emailFooter(): string {
    return `
        <hr style="border: none; border-top: 1px solid #e5e5e5; margin: 32px 0 16px;" />
        <p style="font-size: 12px; color: #666;">
            You're receiving this email because you signed up for PatchNotes.
            <a href="${SETTINGS_URL}">Manage email preferences</a>
        </p>
    `;
}
