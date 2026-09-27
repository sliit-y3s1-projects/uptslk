import { Hash } from "lucide-react";
import {
  GitHubMark,
  PublicHeader,
} from "@/features/landing/LandingPage";

const teamMembers = [
  {
    name: "Chamal Senarathna",
    studentNumber: "IT24103885",
    githubHandle: "chamals3n4",
    githubUrl: "https://github.com/chamals3n4",
  },
  {
    name: "Kishan Ahamed",
    studentNumber: "IT24103829",
    githubHandle: "kishan-ahamed45",
    githubUrl: "https://github.com/kishan-ahamed45",
  },
  {
    name: "Rashmi Thilakarathna",
    studentNumber: "IT24103109",
    githubHandle: "RashmiK0119",
    githubUrl: "https://github.com/RashmiK0119",
  },
  {
    name: "Nadeesha D. Shalom",
    studentNumber: "IT24102244",
    githubHandle: "Nadeesha-D-Shalom",
    githubUrl: "https://github.com/Nadeesha-D-Shalom",
  },
];

export function TeamPage() {
  return (
    <div className="min-h-screen bg-[#f6f5ef] text-[#17171b] lg:h-screen lg:overflow-hidden">
      <PublicHeader />

      <main className="mx-auto max-w-5xl px-5 py-8 sm:px-8 sm:py-10 lg:px-10 lg:py-12">
        <div className="border-b border-[#17171b]/25 pb-4">
          <h1 className="text-3xl font-medium tracking-[-0.04em] sm:text-4xl">
            Team
          </h1>
        </div>

        <section className="mt-6 grid gap-4 sm:grid-cols-2">
          {teamMembers.map((member, index) => (
            <article
              key={member.githubUrl}
              className="rounded-2xl border border-[#17171b]/25 bg-white p-5 transition-colors hover:border-primary/45 sm:p-6"
            >
              <div className="flex items-start justify-between gap-4">
                <h2 className="text-lg font-semibold tracking-[-0.025em] sm:text-xl">
                  {member.name}
                </h2>
                <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-semibold text-white">
                  {String(index + 1).padStart(2, "0")}
                </span>
              </div>

              <div className="mt-5 border-t border-[#17171b]/15 pt-4">
                <div className="flex items-center gap-2 rounded-xl bg-[#f3f2ed] px-3 py-2.5 text-sm text-[#52525b]">
                  <Hash className="size-4 shrink-0 text-primary" />
                  <span className="truncate">{member.studentNumber}</span>
                </div>
              </div>

              <a
                href={member.githubUrl}
                target="_blank"
                rel="noreferrer"
                className="mt-4 flex min-w-0 items-center gap-2 rounded-xl bg-[#f3f2ed] px-3 py-2.5 text-sm text-[#52525b] transition-colors hover:bg-primary/8 hover:text-primary"
              >
                <GitHubMark />
                <span className="truncate">{member.githubHandle}</span>
              </a>
            </article>
          ))}
        </section>
      </main>
    </div>
  );
}
