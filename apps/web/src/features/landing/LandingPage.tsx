import {
  ArrowRight,
  CreditCard,
  Search,
  TicketCheck,
} from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/hooks/useAuth";

export function LandingPage() {
  const { user } = useAuth();

  return (
    <div className="min-h-screen bg-[#f6f5ef] text-[#17171b] lg:h-screen lg:overflow-hidden">
      <PublicHeader />

      <main>
        <section className="mx-auto grid max-w-[1440px] border-x border-[#17171b]/15 lg:h-[calc(100vh-4.5rem)] lg:grid-cols-[1.08fr_0.92fr]">
          <div className="flex flex-col justify-between border-b border-[#17171b]/15 px-6 py-14 sm:px-10 sm:py-18 lg:border-r lg:border-b-0 lg:px-16 lg:py-20 xl:px-20">
            <div>
              <h1 className="max-w-3xl text-[clamp(3.35rem,7vw,7.4rem)] font-medium leading-[0.91] tracking-[-0.065em]">
                Your route.
                <br />
                Your seat.
                <br />
                <span className="text-primary">Sorted.</span>
              </h1>
            </div>

            <div className="mt-14 max-w-xl lg:mt-20">
              <p className="text-lg leading-7 text-[#52525b] sm:text-xl sm:leading-8">
                UPTSLK lets passengers search scheduled departures, reserve
                seats, pay online, and access a digital boarding ticket.
              </p>
              <div className="mt-8 flex flex-wrap items-center gap-4">
                <Button
                  render={<Link to="/reservation" />}
                  size="lg"
                  className="h-14 rounded-full px-7 text-base"
                >
                  Find your departure
                  <ArrowRight className="size-4" />
                </Button>
                {!user && (
                  <Button
                    render={<Link to="/signup" />}
                    size="lg"
                    variant="outline"
                    className="h-14 rounded-full border-[#17171b]/30 bg-transparent px-7 text-base hover:bg-white"
                  >
                    Create an account
                  </Button>
                )}
              </div>
            </div>
          </div>

          <ProductOverview />
        </section>
      </main>
    </div>
  );
}

export function PublicHeader() {
  const { user } = useAuth();

  return (
    <header className="border-b border-[#17171b]/15">
      <div className="mx-auto flex h-18 max-w-[1440px] items-center justify-between px-5 sm:px-8 lg:px-12">
        <Link to="/">
          <span className="text-xl font-semibold tracking-[-0.03em]">UPTSLK</span>
        </Link>

        <nav className="flex items-center gap-2 sm:gap-4" aria-label="Public navigation">
          <Link
            to="/reservation"
            className="hidden rounded-full border border-[#17171b]/25 px-4 py-2 text-sm font-medium transition-colors hover:border-primary/40 hover:bg-white hover:text-primary sm:block"
          >
            Reserve a seat
          </Link>
          <Link
            to="/team"
            className="rounded-full border border-[#17171b]/25 px-4 py-2 text-sm font-medium transition-colors hover:border-primary/40 hover:bg-white hover:text-primary"
          >
            Team
          </Link>
          <Button
            render={
              <a
                href="https://github.com/sliit-y3s1-projects/uptslk"
                target="_blank"
                rel="noreferrer"
                aria-label="View UPTSLK on GitHub"
              />
            }
            variant="outline"
            className="rounded-full border-[#17171b]/25 bg-transparent px-3 hover:bg-white sm:px-4"
          >
            <GitHubMark />
            <span className="hidden md:inline">GitHub</span>
          </Button>
          {user ? (
            <Button
              render={<Link to="/profile" />}
              variant="outline"
              className="rounded-full border-[#17171b]/25 bg-transparent px-5 hover:bg-white"
            >
              My profile
            </Button>
          ) : (
            <Button
              render={<Link to="/login" />}
              variant="outline"
              className="rounded-full border-[#17171b]/25 bg-transparent px-5 hover:bg-white"
            >
              Sign in
            </Button>
          )}
        </nav>
      </div>
    </header>
  );
}

export function GitHubMark() {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className="size-4 fill-current"
    >
      <path d="M12 .7a11.5 11.5 0 0 0-3.64 22.41c.58.1.79-.25.79-.56v-2.24c-3.22.7-3.9-1.37-3.9-1.37-.53-1.34-1.29-1.7-1.29-1.7-1.05-.72.08-.71.08-.71 1.17.08 1.78 1.2 1.78 1.2 1.04 1.78 2.72 1.27 3.38.97.1-.75.4-1.27.74-1.56-2.57-.29-5.27-1.28-5.27-5.68 0-1.25.45-2.28 1.2-3.08-.12-.29-.52-1.47.11-3.05 0 0 .98-.32 3.16 1.17A10.96 10.96 0 0 1 12 6.12c.98 0 1.95.13 2.86.38 2.19-1.49 3.17-1.17 3.17-1.17.63 1.58.23 2.76.11 3.05.75.8 1.2 1.83 1.2 3.08 0 4.41-2.71 5.38-5.29 5.67.42.36.79 1.06.79 2.14v3.28c0 .31.21.67.8.56A11.5 11.5 0 0 0 12 .7Z" />
    </svg>
  );
}

function ProductOverview() {
  return (
    <div className="relative flex min-h-[560px] overflow-hidden bg-primary p-6 text-white sm:p-10 lg:min-h-full lg:p-12 xl:p-16">
      <div aria-hidden="true" className="absolute -right-28 -top-28 size-80 rounded-full border border-white/15" />
      <div aria-hidden="true" className="absolute -right-6 -top-6 size-40 rounded-full border border-white/15" />

      <div className="relative flex w-full items-center">
        <div className="w-full">
          <h2 className="max-w-md text-3xl font-medium leading-tight tracking-[-0.035em] sm:text-4xl">
            Everything needed from search to boarding.
          </h2>
          <div className="mt-8 border-t border-white/25">
            <ProductStep
              number="01"
              icon={Search}
              title="Search departures"
              detail="Compare scheduled buses by route and travel date."
            />
            <ProductStep
              number="02"
              icon={CreditCard}
              title="Reserve and pay"
              detail="Confirm passenger details and complete secure payment."
            />
            <ProductStep
              number="03"
              icon={TicketCheck}
              title="Use your ticket"
              detail="Open your booking and present its digital boarding pass."
            />
          </div>
        </div>
      </div>
    </div>
  );
}

function ProductStep({
  number,
  icon: Icon,
  title,
  detail,
}: {
  number: string;
  icon: typeof Search;
  title: string;
  detail: string;
}) {
  return (
    <div className="grid grid-cols-[2rem_2.5rem_1fr] items-start gap-3 border-b border-white/25 py-5">
      <span className="pt-1 text-xs font-semibold text-white/45">{number}</span>
      <Icon className="size-5 text-[#d7ff72]" />
      <div className="min-w-0">
        <p className="font-medium">{title}</p>
        <p className="mt-1 text-sm leading-5 text-white/60">{detail}</p>
      </div>
    </div>
  );
}
