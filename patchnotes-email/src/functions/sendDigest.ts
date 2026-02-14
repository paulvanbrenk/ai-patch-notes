import { app, InvocationContext, Timer } from "@azure/functions";
import { resend, FROM_ADDRESS, escapeHtml, emailFooter } from "../lib/resend";

interface UserDigest {
    email: string;
    name: string;
    releases: Array<{
        packageName: string;
        version: string;
        summary: string;
    }>;
}

export async function sendDigest(
    myTimer: Timer,
    context: InvocationContext
): Promise<void> {
    context.log("sendDigest triggered at", new Date().toISOString());

    const apiUrl = process.env.PATCHNOTES_API_URL;
    if (!apiUrl) {
        context.error("PATCHNOTES_API_URL not configured");
        return;
    }

    let users: UserDigest[];
    try {
        const response = await fetch(`${apiUrl}/api/digest/pending`);
        if (!response.ok) {
            context.error(`API returned ${response.status}`);
            return;
        }
        users = await response.json() as UserDigest[];
    } catch (err) {
        context.error("Failed to fetch digest data:", err);
        return;
    }

    if (users.length === 0) {
        context.log("No users with pending digest items");
        return;
    }

    context.log(`Sending digests to ${users.length} users`);

    for (const user of users) {
        if (user.releases.length === 0) continue;

        // TODO: Replace with React Email WeeklyDigest template when available
        const releaseList = user.releases
            .map((r) => `<li><strong>${escapeHtml(r.packageName)} ${escapeHtml(r.version)}</strong>: ${escapeHtml(r.summary)}</li>`)
            .join("\n");

        const html = `
            <h1>Your Weekly PatchNotes Digest</h1>
            <p>Hi ${escapeHtml(user.name)}, here's what happened this week with the packages you're watching:</p>
            <ul>${releaseList}</ul>
            <p><a href="${apiUrl}">View all updates on PatchNotes</a></p>
            ${emailFooter()}
        `;

        try {
            const { error } = await resend.emails.send({
                from: FROM_ADDRESS,
                to: user.email,
                subject: `Your Weekly PatchNotes Digest — ${user.releases.length} updates`,
                html,
            });

            if (error) {
                context.error(`Failed to send digest to ${user.email}:`, error);
            } else {
                context.log(`Digest sent to ${user.email}`);
            }
        } catch (err) {
            context.error(`Error sending to ${user.email}:`, err);
        }
    }
}

// Runs every Monday at 9:00 AM UTC
app.timer("sendDigest", {
    schedule: "0 0 9 * * 1",
    handler: sendDigest,
});
