"use client";

import {useEffect, useState} from "react";
import WorldCard, {WorldCardSkeleton} from "@/components/WorldCard";
import Dict = NodeJS.Dict;

const PAGE_API = "/api/worlddatalist/";
const PAGE_SIZE = 10;

type WorldMetadata = {
    worldId: string;
    worldName: string;
    authorId: string;
    authorName: string;
    imageUrl: string;
    capacity: number;
    visits: number;
    favorites: number;
    heat: number;
    popularity: number;
    tags: string[];
    updatedAt: Date;
}

export type WorldPayload = {
    worldId: string;
    metadata: WorldMetadata;
    imageList: Dict<string>[];
    category: string[] | null;
    description: string | null;
    dataCreatedAt: Date;
    lastFolderModifiedAt: Date;
} | null;

function encodeCursor(dt: Date, worldId: string): string {
    // ISO8601 UTC 문자열로 변환
    const dtIso = dt.toISOString(); // ex) "2025-09-25T04:35:46.440Z"
    const raw = `${dtIso}|${worldId}`;

    // UTF-8 → Base64
    return btoa(unescape(encodeURIComponent(raw)));
}

export default function Page() {
    const skeletons = Array.from({length: PAGE_SIZE});
    const [worlds, setWorlds] = useState<WorldPayload[]>([]);
    const [worldCardsLoading, setWorldCardsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // DEV
    useEffect(() => {
        console.log('worlds updated!', worlds);
    }, [worlds]);

    useEffect(() => {
        let aborted = false;

        async function load() {

            try {
                setWorldCardsLoading(true);
                setError(null);
                const res = await fetch(PAGE_API + `?pageCount=${PAGE_SIZE}`, {cache: "no-store"});
                if (!res.ok) {
                    throw new Error(`HTTP ${res.status}`);
                }
                const data: WorldPayload[] = await res.json();
                if (!aborted)
                    setWorlds(data);
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
                    {worlds.filter(notNull).map((w) => (
                        <WorldCard
                            key={w.worldId}
                            worldId={w.metadata.worldId}
                            worldName={w.metadata.worldName}
                            authorId={w.metadata.authorId}
                            authorName={w.metadata.authorName}
                            imageUrl={w.metadata.imageUrl}
                            capacity={w.metadata.capacity}
                            visits={w.metadata.visits}
                            favorites={w.metadata.favorites}
                            heat={w.metadata.heat}
                            popularity={w.metadata.popularity}
                            tags={w.metadata.tags}
                            imageList={w.imageList}
                            category={w.category}
                            description={w.description}
                            dataCreatedAt={w.dataCreatedAt}
                            lastFolderModifiedAt={w.lastFolderModifiedAt}
                        />
                    ))}
                </section>
            }
        </main>
    )
        ;
}

function notNull<T>(item: T | null): item is T {
    return item !== null;
}