"use client";
import { motion } from "framer-motion";
import Link from "next/link";

const fade = {
  initial: { opacity: 0, y: 16 },
  whileInView: { opacity: 1, y: 0 },
  viewport: { once: true },
  transition: { duration: 0.55 },
};

const RELEASE_TAG = "v0.3.0";
const GITHUB_REPO = "https://github.com/OWNER/ASLAN-Client";

export default function Home() {
  return (
    <main className="min-h-screen bg-[#0a0a10] text-[#ebebf5]">
      <motion.section {...fade} className="mx-auto max-w-4xl px-6 pt-24 text-center">
        <h1 className="bg-gradient-to-r from-[#a78bfa] to-[#38bdf8] bg-clip-text text-6xl font-extrabold tracking-wide text-transparent">
          ASLAN CLIENT
        </h1>
        <p className="mt-3 text-xl font-semibold">Your Minecraft. Your Performance. Your Rules.</p>
        <p className="mt-2 text-[#94a3b8]">
          The next generation of Minecraft performance, customization and smooth gameplay.
        </p>
        <div className="mt-8 flex justify-center gap-4">
          <a
            href={`${GITHUB_REPO}/releases/tag/${RELEASE_TAG}`}
            className="rounded-xl bg-gradient-to-r from-[#6d48f2] to-[#3aa8f0] px-7 py-3 font-bold"
          >
            Download for Windows
          </a>
          <a href="#performance" className="rounded-xl border border-[#7c5dfa] px-7 py-3 font-bold">
            Explore Features
          </a>
        </div>
      </motion.section>

      <motion.section id="performance" {...fade} className="mx-auto max-w-4xl px-6 py-16">
        <h2 className="text-3xl font-bold text-[#a78bfa]">Performance Engine</h2>
        <p className="mt-2 text-[#94a3b8]">
          Vier Profile (MAX FPS, BALANCED, QUALITY, CUSTOM), Hardware-Erkennung und ehrliche
          Benchmarks — Avg-FPS, 1%-Lows, Frametimes. Keine erfundenen Zahlen.
        </p>
      </motion.section>

      <motion.section {...fade} className="mx-auto max-w-4xl px-6 pb-16">
        <h2 className="text-3xl font-bold text-[#a78bfa]">Customization</h2>
        <p className="mt-2 text-[#94a3b8]">
          Eigenes Hauptmenü, HUD-Editor mit Profilen, Quick Settings, Themes und optionale
          Cosmetics — alles deaktivierbar.
        </p>
        <p className="mt-6">
          <Link href="/downloads" className="text-[#a78bfa] underline">
            Downloads mit SHA-256-Prüfsummen
          </Link>
        </p>
      </motion.section>

      <footer className="border-t border-[#26263a] px-6 py-8 text-center text-sm text-[#94a3b8]">
        ASLAN CLIENT ist ein unabhängiges Drittanbieterprojekt, nicht von Mojang/Microsoft.
      </footer>
    </main>
  );
}
