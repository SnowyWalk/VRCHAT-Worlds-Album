// app/layout.tsx
import "./globals.css"
import type {Metadata} from "next"
import {ThemeProvider} from "@/components/theme-provider"
import ThemeToggle from "@/components/theme-toggle";

export const metadata: Metadata = {title: "My App"}

export default function RootLayout({children}: { children: React.ReactNode }) {
    return (
        <html lang="ko" suppressHydrationWarning>
        <body>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
            <header className="border-b">
                <div className="mx-auto max-w-7xl px-6 py-5">
                    <div className="grid grid-cols-3 items-center">
                        {/* 좌측 자리(타이틀을 정확히 중앙에 두기 위한 placeholder) */}
                        <div aria-hidden className="justify-self-start">
                            {/* ThemeToggle과 동일한 크기의 아이콘 버튼 자리를 확보 */}
                            <div className="w-9 h-9" />
                        </div>

                        {/* 중앙 타이틀 */}
                        <h1 className="justify-self-center text-2xl sm:text-3xl md:text-4xl font-extrabold tracking-tight text-center">
                            VRCHAT Worlds Album
                        </h1>

                        {/* 우측 테마 토글(완전히 오른쪽 끝) */}
                        <div className="justify-self-end">
                            <ThemeToggle />
                        </div>
                    </div>
                </div>
            </header>
            {children}
        </ThemeProvider>
        </body>
        </html>
    )
}
