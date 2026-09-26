import { ArrowRight, Check, MapPin, Ticket, UserRound } from "lucide-react";
import { Link, useLocation } from "react-router";
import { Button } from "@/components/ui/button";

export function OnboardingPage() {
  const profilePhotoWarning = (
    useLocation().state as { profilePhotoWarning?: string } | null
  )?.profilePhotoWarning;

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-12">
      <section className="w-full max-w-3xl rounded-3xl border border-slate-200 bg-white p-8 sm:p-12">
        <div className="mb-10 flex items-start justify-between gap-8">
          <div>
            <p className="text-base font-medium text-primary">
              Welcome to UPTSLK
            </p>
            <h1 className="mt-3 text-4xl font-semibold tracking-tight text-slate-950">
              Your journeys, simplified.
            </h1>
            <p className="mt-4 max-w-xl text-base leading-7 text-slate-500">
              Set up your commuter profile once, then search routes, reserve
              seats, and keep every ticket in one place.
            </p>
          </div>
          <div className="hidden size-14 items-center justify-center rounded-2xl bg-primary/10 text-primary sm:flex">
            <UserRound className="size-7" />
          </div>
        </div>
        {profilePhotoWarning && (
          <p className="mb-6 rounded-xl border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900">
            {profilePhotoWarning} You can try again from My Profile.
          </p>
        )}
        <div className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-2xl border border-slate-200 p-5">
            <MapPin className="size-6 text-primary" />
            <p className="mt-4 text-base font-medium">Find routes</p>
            <p className="mt-2 text-sm leading-6 text-slate-500">
              Search active services by origin and destination.
            </p>
          </div>
          <div className="rounded-2xl border border-slate-200 p-5">
            <Ticket className="size-6 text-primary" />
            <p className="mt-4 text-base font-medium">Reserve seats</p>
            <p className="mt-2 text-sm leading-6 text-slate-500">
              Choose a departure and keep your booking details handy.
            </p>
          </div>
          <div className="rounded-2xl border border-slate-200 p-5">
            <Check className="size-6 text-primary" />
            <p className="mt-4 text-base font-medium">Travel with confidence</p>
            <p className="mt-2 text-sm leading-6 text-slate-500">
              Access tickets and journey information anytime.
            </p>
          </div>
        </div>
        <div className="mt-10 flex justify-end">
          <Button
            render={<Link to="/" />}
            className="h-12 rounded-xl bg-primary px-6 text-base"
          >
            Start booking <ArrowRight />
          </Button>
        </div>
      </section>
    </main>
  );
}
