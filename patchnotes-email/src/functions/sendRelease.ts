import { app, HttpRequest, HttpResponseInit, InvocationContext } from "@azure/functions";
import { resend, FROM_ADDRESS, escapeHtml, emailFooter } from "../lib/resend";

interface ReleaseRequest {
    email: string;
    packageName: string;
    version: string;
    summary: string;
}

export async function sendRelease(
    request: HttpRequest,
    context: InvocationContext
): Promise<HttpResponseInit> {
    context.log("sendRelease triggered");

    let body: ReleaseRequest;
    try {
        body = (await request.json()) as ReleaseRequest;
    } catch {
        return { status: 400, body: "Invalid JSON body" };
    }

    if (!body.email || !body.packageName || !body.version || !body.summary) {
        return { status: 400, body: "Missing required fields: email, packageName, version, summary" };
    }

    try {
        // TODO: Replace with React Email ReleaseNotification template when available
        const pkg = escapeHtml(body.packageName);
        const ver = escapeHtml(body.version);
        const html = `
            <h1>New Release: ${pkg} ${ver}</h1>
            <p>${escapeHtml(body.summary)}</p>
            <p><a href="https://patchnotes.dev">View on PatchNotes</a></p>
            ${emailFooter()}
        `;

        const { error } = await resend.emails.send({
            from: FROM_ADDRESS,
            to: body.email,
            subject: `${body.packageName} ${body.version} released`,
            html,
        });

        if (error) {
            context.error("Resend error:", error);
            return { status: 500, body: `Failed to send email: ${error.message}` };
        }

        return { status: 200, body: "Release notification sent" };
    } catch (err) {
        context.error("Unexpected error:", err);
        return { status: 500, body: "Internal server error" };
    }
}

app.http("sendRelease", {
    methods: ["POST"],
    authLevel: "function",
    handler: sendRelease,
});
