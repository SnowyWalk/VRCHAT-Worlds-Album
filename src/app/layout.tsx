// app/layout.tsx
import "./globals.css"
import type {Metadata} from "next"
import {ThemeProvider} from "@/components/theme-provider"
import ThemeToggle from "@/components/theme-toggle";
import QueryProvider from "@/query/query-provider";
import {ReactQueryDevtools} from "@tanstack/react-query-devtools";

export const metadata: Metadata = {title: "My App"}

export default function RootLayout({children}: { children: React.ReactNode }) {
    return (
        <html lang="ko" suppressHydrationWarning>
        <body>
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
            <QueryProvider>
                <header className="max-w-[1800px] px-4 mx-auto my-6">
                    <div className="flex items-center justify-between">
                        {/* 타이틀: 왼쪽 정렬 */}
                        <h1 className="text-2xl sm:text-3xl md:text-4xl font-extrabold tracking-tight mt-4.5">
                            VRCHAT Worlds Album
                        </h1>
                        {/* 테마 버튼: 오른쪽 끝 */}
                        <ThemeToggle/>
                    </div>
                </header>
                <section className="max-w-[1800px] px-4 mx-auto space-y-6">
                    {children}
                </section>
                <ReactQueryDevtools initialIsOpen={false}/>
            </QueryProvider>
        </ThemeProvider>
        </body>
        </html>
    )
}
