"use client";

import {useEffect, useState} from "react";
import WorldCard, {WorldCardSkeleton} from "@/components/WorldCard";
import Dict = NodeJS.Dict;
import ImageView from "@/components/ImageView";
import {PeekCarousel} from "@/components/PeekCarousel";
import {useQuery} from "@tanstack/react-query";
import Link from "next/link";
import {useParams, useRouter, useSearchParams} from "next/navigation";
import {Button} from "@/components/ui/button";
import {keepPreviousData} from "@tanstack/query-core";
import {getCategoryUrl} from "@/utils/url-util";

const PAGE_API = "/api/worlddatalist/";
const DEFAULT_PAGE_INDEX = 1;
const DEFAULT_PAGE_SIZE = 10;

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


export default function Page() {
    const router = useRouter();

    const params = useParams<{category: string, page: string, pageSize: string}>();
    params.page = String(Math.max(
        1,
        Number(params.page)
    ));

    const skeletons = Array.from({length: Number(params.pageSize)});

    const {
        data: worlds,
        isFetching: isWorldsFetching,
        isError: isWorldsFetchError,
        error: worldsFetchError
    } = useQuery<WorldPayload[]>({
        queryKey: ['worlds', params.page, params.pageSize],
        queryFn: async () => fetch(`${PAGE_API}?page=${params.page}&pageSize=${params.pageSize}`).then(res => res.json()),
        refetchOnMount: false,
        refetchOnReconnect: true,
        refetchOnWindowFocus: false,
        placeholderData: undefined,
        staleTime: 1000 *  8, // dev일 때는 줄이는게 좋겠다.
    });

    const [viewImageList, setViewImageList] = useState<[Dict<string>[] | null, number]>([null, 0]);
    const [viewImageList_list, viewImageList_index] = viewImageList;

    function setPage(page: number) {
        const newPage = Math.max(1, page);
        router.push(getCategoryUrl(params.category, newPage, Number(params.pageSize)));
    }

    return (
        <main>

            <Button onClick={() => setPage(Number(params.page) - 1)}>이전페이지</Button>
            <Button onClick={() => setPage(Number(params.page) + 1)}>다음페이지</Button>

            {
                isWorldsFetching && !worlds &&
                (
                    <section
                        className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 2xl:grid-cols-5 gap-7">
                        {skeletons.map((_, i) => <WorldCardSkeleton key={i}/>)}
                    </section>
                )
            }

            {
                isWorldsFetchError &&
                (
                    <p className="text-red-600">불러오기 오류: {JSON.stringify(worldsFetchError)}</p>
                )
            }

            {
                worlds &&
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
                            onClickThumbnailAction={onClickThumbnail}
                        />
                    ))}
                </section>
            }

            {
                <section className={`${viewImageList_list ? "block" : "hidden"}`}>
                    <ImageView imageList={viewImageList_list} imageIndex={viewImageList_index}
                               onESCAction={onESCOnImageView}/>
                </section>
            }
        </main>
    );

    function onClickThumbnail([imageList, index]: [Dict<string>[], number]) {
        setViewImageList([imageList, index]);
    }

    function onESCOnImageView() {
        setViewImageList([null, 0]);
    }
}

function notNull<T>(item: T | null): item is T {
    return item !== null;
}

// function encodeCursor(dt: Date, worldId: string): string {
//     // ISO8601 UTC 문자열로 변환
//     const dtIso = dt.toISOString(); // ex) "2025-09-25T04:35:46.440Z"
//     const raw = `${dtIso}|${worldId}`;
//
//     // UTF-8 → Base64
//     return btoa(unescape(encodeURIComponent(raw)));
// }