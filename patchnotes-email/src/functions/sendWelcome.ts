import { app, HttpRequest, HttpResponseInit, InvocationContext } from "@azure/functions";
import { resend, FROM_ADDRESS, escapeHtml, emailFooter, sanitizeSubject, isValidEmail } from "../lib/resend";

interface WelcomeRequest {
    email: string;
    name: string;
}

export async function sendWelcome(
    request: HttpRequest,
    context: InvocationContext
): Promise<HttpResponseInit> {
    context.log("sendWelcome triggered");

    let body: WelcomeRequest;
    try {
        body = (await request.json()) as WelcomeRequest;
    } catch {
        return { status: 400, body: "Invalid JSON body" };
    }

    if (!body.email || !body.name) {
        return { status: 400, body: "Missing required fields: email, name" };
    }

    if (!isValidEmail(body.email)) {
        return { status: 400, body: "Invalid email address format" };
    }

    try {
        // TODO: Replace with React Email template when available
        const name = escapeHtml(body.name);
        const html = `
            <h1>Welcome to PatchNotes, ${name}!</h1>
            <p>You're all set to receive release notifications for the packages you care about.</p>
            <p>Head to your dashboard to start watching packages.</p>
            ${emailFooter()}
        `;

        const { error } = await resend.emails.send({
            from: FROM_ADDRESS,
            to: body.email,
            subject: sanitizeSubject(`Welcome to PatchNotes, ${body.name}!`),
            html,
        });

        if (error) {
            context.error("Resend error:", error);
            return { status: 500, body: `Failed to send email: ${error.message}` };
        }

        return { status: 200, body: "Welcome email sent" };
    } catch (err) {
        context.error("Unexpected error:", err);
        return { status: 500, body: "Internal server error" };
    }
}

app.http("sendWelcome", {
    methods: ["POST"],
    authLevel: "function",
    handler: sendWelcome,
});
