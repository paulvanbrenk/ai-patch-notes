import { describe, it, expect, vi, beforeEach } from "vitest";

const { mockSend, mockFindMany } = vi.hoisted(() => ({
    mockSend: vi.fn(),
    mockFindMany: vi.fn(),
}));

vi.mock("../lib/resend", () => ({
    resend: { emails: { send: mockSend } },
    FROM_ADDRESS: "PatchNotes <notifications@patchnotes.dev>",
    escapeHtml: (s: string) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"),
    emailFooter: () => "<footer/>",
    sanitizeSubject: (s: string) => s.replace(/[\r\n]+/g, " ").trim(),
    isValidEmail: (email: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email),
}));

vi.mock("../lib/prisma", () => ({
    getPrismaClient: () => ({ users: { findMany: mockFindMany } }),
}));

import { sendDigest } from "./sendDigest";

function makeContext() {
    return {
        log: vi.fn(),
        warn: vi.fn(),
        error: vi.fn(),
    } as any;
}

function makeTimer(): any {
    return { isPastDue: false };
}

function makeUser(email: string, name: string, releases: Array<{ tag: string; major: number }>) {
    return {
        Email: email,
        Name: name,
        Watchlists: [
            {
                Packages: {
                    Name: "test-package",
                    Releases: releases.map((r) => ({
                        Tag: r.tag,
                        MajorVersion: r.major,
                        IsPrerelease: false,
                    })),
                    ReleaseSummaries: releases.map((r) => ({
                        Summary: `Summary for ${r.tag}`,
                        MajorVersion: r.major,
                        IsPrerelease: false,
                    })),
                },
            },
        ],
    };
}

describe("sendDigest", () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it("returns early when no users have pending items", async () => {
        mockFindMany.mockResolvedValue([]);
        const context = makeContext();

        await sendDigest(makeTimer(), context);

        expect(mockSend).not.toHaveBeenCalled();
        expect(context.log).toHaveBeenCalledWith("No users with pending digest items");
    });

    it("sends digests to all users and logs summary on full success", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("a@test.com", "Alice", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("b@test.com", "Bob", [{ tag: "v2.0.0", major: 2 }]),
        ]);
        mockSend.mockResolvedValue({ error: null });
        const context = makeContext();

        await sendDigest(makeTimer(), context);

        expect(mockSend).toHaveBeenCalledTimes(2);
        expect(context.log).toHaveBeenCalledWith(
            "Digest summary: 2 sent, 0 failed, 0 skipped out of 2 users"
        );
    });

    it("throws when some sends fail with API error", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("a@test.com", "Alice", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("b@test.com", "Bob", [{ tag: "v2.0.0", major: 2 }]),
            makeUser("c@test.com", "Carol", [{ tag: "v3.0.0", major: 3 }]),
        ]);
        mockSend
            .mockResolvedValueOnce({ error: null })
            .mockResolvedValueOnce({ error: { message: "rate limited" } })
            .mockResolvedValueOnce({ error: null });
        const context = makeContext();

        await expect(sendDigest(makeTimer(), context)).rejects.toThrow(
            "Digest send partially failed: 1/3 sends failed. Failed: b@test.com"
        );
        expect(context.log).toHaveBeenCalledWith(
            "Digest summary: 2 sent, 1 failed, 0 skipped out of 3 users"
        );
    });

    it("throws when sends throw exceptions", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("a@test.com", "Alice", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("b@test.com", "Bob", [{ tag: "v2.0.0", major: 2 }]),
        ]);
        mockSend
            .mockResolvedValueOnce({ error: null })
            .mockRejectedValueOnce(new Error("network error"));
        const context = makeContext();

        await expect(sendDigest(makeTimer(), context)).rejects.toThrow(
            "Digest send partially failed: 1/2 sends failed. Failed: b@test.com"
        );
    });

    it("throws when all sends fail", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("a@test.com", "Alice", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("b@test.com", "Bob", [{ tag: "v2.0.0", major: 2 }]),
        ]);
        mockSend.mockResolvedValue({ error: { message: "service down" } });
        const context = makeContext();

        await expect(sendDigest(makeTimer(), context)).rejects.toThrow(
            "Digest send partially failed: 2/2 sends failed. Failed: a@test.com, b@test.com"
        );
    });

    it("skips users with invalid emails and counts them", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("valid@test.com", "Valid", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("not-an-email", "Invalid", [{ tag: "v2.0.0", major: 2 }]),
        ]);
        mockSend.mockResolvedValue({ error: null });
        const context = makeContext();

        await sendDigest(makeTimer(), context);

        expect(mockSend).toHaveBeenCalledTimes(1);
        expect(context.warn).toHaveBeenCalledWith(
            "Skipping digest for user with invalid email: not-an-email"
        );
        expect(context.log).toHaveBeenCalledWith(
            "Digest summary: 1 sent, 0 failed, 1 skipped out of 2 users"
        );
    });

    it("skips users with no releases and counts them", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("a@test.com", "Alice", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("b@test.com", "Bob", []),  // no releases
        ]);
        mockSend.mockResolvedValue({ error: null });
        const context = makeContext();

        await sendDigest(makeTimer(), context);

        expect(mockSend).toHaveBeenCalledTimes(1);
        expect(context.log).toHaveBeenCalledWith(
            "Digest summary: 1 sent, 0 failed, 1 skipped out of 2 users"
        );
    });

    it("continues sending to remaining users after a failure", async () => {
        mockFindMany.mockResolvedValue([
            makeUser("a@test.com", "Alice", [{ tag: "v1.0.0", major: 1 }]),
            makeUser("b@test.com", "Bob", [{ tag: "v2.0.0", major: 2 }]),
            makeUser("c@test.com", "Carol", [{ tag: "v3.0.0", major: 3 }]),
        ]);
        mockSend
            .mockRejectedValueOnce(new Error("fail"))
            .mockResolvedValueOnce({ error: null })
            .mockResolvedValueOnce({ error: null });
        const context = makeContext();

        await expect(sendDigest(makeTimer(), context)).rejects.toThrow();
        // All 3 users should have been attempted (not short-circuited)
        expect(mockSend).toHaveBeenCalledTimes(3);
    });
});
