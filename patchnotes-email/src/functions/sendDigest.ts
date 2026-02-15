import { app, InvocationContext, Timer } from "@azure/functions";
import { resend, FROM_ADDRESS, escapeHtml, emailFooter } from "../lib/resend";
import { getPrismaClient } from "../lib/prisma";

const DIGEST_WINDOW_DAYS = 7;

export async function sendDigest(
    myTimer: Timer,
    context: InvocationContext
): Promise<void> {
    context.log("sendDigest triggered at", new Date().toISOString());

    const db = getPrismaClient();
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - DIGEST_WINDOW_DAYS);

    const users = await db.users.findMany({
        where: {
            EmailDigestEnabled: true,
            Email: { not: null },
            Watchlists: {
                some: {
                    Packages: {
                        Releases: {
                            some: { PublishedAt: { gte: cutoff } },
                        },
                    },
                },
            },
        },
        select: {
            Email: true,
            Name: true,
            Watchlists: {
                select: {
                    Packages: {
                        select: {
                            Name: true,
                            Releases: {
                                where: { PublishedAt: { gte: cutoff } },
                                orderBy: { PublishedAt: "desc" },
                                select: {
                                    Tag: true,
                                    MajorVersion: true,
                                    IsPrerelease: true,
                                },
                            },
                            ReleaseSummaries: {
                                select: {
                                    Summary: true,
                                    MajorVersion: true,
                                    IsPrerelease: true,
                                },
                            },
                        },
                    },
                },
            },
        },
    });

    if (users.length === 0) {
        context.log("No users with pending digest items");
        return;
    }

    context.log(`Sending digests to ${users.length} users`);

    for (const user of users) {
        const releases: Array<{ packageName: string; version: string; summary: string }> = [];

        for (const watch of user.Watchlists) {
            for (const release of watch.Packages.Releases) {
                const matchingSummary = watch.Packages.ReleaseSummaries.find(
                    (s) => s.MajorVersion === release.MajorVersion && s.IsPrerelease === release.IsPrerelease
                );
                releases.push({
                    packageName: watch.Packages.Name,
                    version: release.Tag,
                    summary: matchingSummary?.Summary ?? "",
                });
            }
        }

        if (releases.length === 0) continue;

        // TODO: Replace with React Email WeeklyDigest template when available
        const releaseList = releases
            .map((r) => `<li><strong>${escapeHtml(r.packageName)} ${escapeHtml(r.version)}</strong>: ${escapeHtml(r.summary)}</li>`)
            .join("\n");

        const html = `
            <h1>Your Weekly PatchNotes Digest</h1>
            <p>Hi ${escapeHtml(user.Name ?? "there")}, here's what happened this week with the packages you're watching:</p>
            <ul>${releaseList}</ul>
            <p><a href="https://myreleasenotes.ai">View all updates on PatchNotes</a></p>
            ${emailFooter()}
        `;

        try {
            const { error } = await resend.emails.send({
                from: FROM_ADDRESS,
                to: user.Email!,
                subject: `Your Weekly PatchNotes Digest — ${releases.length} updates`,
                html,
            });

            if (error) {
                context.error(`Failed to send digest to ${user.Email}:`, error);
            } else {
                context.log(`Digest sent to ${user.Email}`);
            }
        } catch (err) {
            context.error(`Error sending to ${user.Email}:`, err);
        }
    }
}

// Runs every Monday at 9:00 AM UTC
app.timer("sendDigest", {
    schedule: "0 0 9 * * 1",
    handler: sendDigest,
});
