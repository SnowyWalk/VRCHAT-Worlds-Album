"use client";

import {useEffect, useState} from "react";
import WorldCard, {WorldCardSkeleton} from "@/components/WorldCard";
import Dict = NodeJS.Dict;

const PAGE_API = "/api/page/";

export type WorldPayload = {
    id: string;
    name: string;
    authorId: string;
    authorName: string;
    imageUrl: string;
    capacity: number;
    visits: number;
    favorites: number;
    heat: number;
    popularity: number;
    tags: string[];
    images: Dict<string>[];
} | null;

function encodeCursor(dt: Date, worldId: string): string {
    // ISO8601 UTC 문자열로 변환
    const dtIso = dt.toISOString(); // ex) "2025-09-25T04:35:46.440Z"
    const raw = `${dtIso}|${worldId}`;

    // UTF-8 → Base64
    return btoa(unescape(encodeURIComponent(raw)));
}

export default function Page() {
    const skeletons = Array.from({length: 10});
    const [worlds, setWorlds] = useState<WorldPayload[]>([]);
    const [worldCardsLoading, setWorldCardsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let aborted = false;

        async function load() {

            try {
                setWorldCardsLoading(true);
                setError(null);
                const res = await fetch(PAGE_API + "0?page_size=28", {cache: "no-store"});
                if (!res.ok) {
                    throw new Error(`HTTP ${res.status}`);
                }
                const data: WorldPayload[] = await res.json();
                console.log(data)
                if (!aborted) setWorlds(Array.isArray(data) ? data : []);
            } catch (e) {
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                if (!aborted) { // @ts-expect-error
                    setError(e?.message ?? "요청 실패");
                }
            } finally {
                if (!aborted) setWorldCardsLoading(false);
            }
        }

        load();
        return () => {
            aborted = true;
        };
    }, []);

    const validWorlds = worlds.filter(Boolean) as Exclude<WorldPayload, null>[];

    return (
        <main className="mx-auto max-w-[1800px] p-4 space-y-6">
            {
                worldCardsLoading &&
                (
                    <section
                        className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-5 gap-7">
                        {skeletons.map((_, i) => <WorldCardSkeleton key={i}/>)}
                    </section>
                )
            }

            {
                error &&
                (
                    <p className="text-red-600">불러오기 오류: {error}</p>
                )
            }

            {
                !worldCardsLoading &&
                <section
                    className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-5 gap-7">
                    {validWorlds.map((w) => (
                        <WorldCard
                            key={w.id}
                            id={w.id}
                            name={w.name}
                            authorId={w.authorId}
                            authorName={w.authorName}
                            imageUrl={w.imageUrl}
                            capacity={w.capacity}
                            visits={w.visits}
                            favorites={w.favorites}
                            heat={w.heat}
                            popularity={w.popularity}
                            tags={w.tags}
                            images={w.images}
                        />
                    ))}
                </section>
            }
        </main>
    );
}