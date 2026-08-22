import { SignInButton } from "@thunderid/react";

import { Button } from "@/components/ui/button";

export function SignInPanel() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6">
      <section className="flex w-full max-w-sm flex-col items-center gap-6 rounded-xl border bg-card p-8 text-center">
        <div className="space-y-2">
          <h1 className="text-2xl font-semibold tracking-tight">UPTS</h1>
          <p className="text-sm leading-6 text-muted-foreground">
            Sign in to continue to the operations dashboard.
          </p>
        </div>

        <SignInButton>
          <Button className="w-full">Sign In</Button>
        </SignInButton>
      </section>
    </main>
  );
}
