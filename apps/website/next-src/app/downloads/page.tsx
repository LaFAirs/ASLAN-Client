import Link from "next/link";

export default function Downloads() {
  const tag = "v0.3.0";
  const repo = "https://github.com/OWNER/ASLAN-Client";
  return (
    <main className="mx-auto max-w-3xl px-6 py-16 text-[#ebebf5]">
      <h1 className="text-4xl font-bold text-[#a78bfa]">Downloads</h1>
      <p className="mt-2 text-[#94a3b8]">
        Echte Builds aus GitHub Releases — inklusive SHA-256-Prüfsummen.
      </p>
      <a
        href={`${repo}/releases/tag/${tag}`}
        className="mt-6 inline-block rounded-xl bg-gradient-to-r from-[#6d48f2] to-[#3aa8f0] px-7 py-3 font-bold"
      >
        Download for Windows ({tag})
      </a>
      <p className="mt-6">
        <Link href="/" className="text-[#a78bfa] underline">
          Zurück zur Startseite
        </Link>
      </p>
    </main>
  );
}
