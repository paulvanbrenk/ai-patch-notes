import { app, InvocationContext, Timer } from "@azure/functions";

export async function SendEmails(myTimer: Timer, context: InvocationContext): Promise<void> {
    context.log('SendEmails triggered at', new Date().toISOString());

    // TODO: Query users with email notifications enabled
    // TODO: Gather new releases since last email
    // TODO: Render and send emails
}

// Runs daily at 4:00 AM ET (09:00 UTC)
app.timer('SendEmails', {
    schedule: '0 0 9 * * *',
    handler: SendEmails
});
