import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "ASLAN CLIENT — Your Minecraft. Your Performance. Your Rules.",
  description:
    "The next generation of Minecraft performance, customization and smooth gameplay. Independent third-party project.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="de">
      <body>{children}</body>
    </html>
  );
}
