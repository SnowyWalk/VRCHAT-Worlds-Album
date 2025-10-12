"use client";

import Image from "next/image";
import {WorldPayload} from "@/schemas/WorldPayload";
import {useQuery} from "@tanstack/react-query";
import {getPageApi} from "@/utils/server-api";
import {DEFAULT_CATEGORY, DEFAULT_PAGE_NUMBER, DEFAULT_PAGE_SIZE} from "@/utils/consts";
import {notNull} from "@/utils/common-util";
import {Table, TableBody, TableCell, TableHead, TableHeader, TableRow} from "@/components/ui/table";
import {Spinner} from "@/components/ui/spinner";
import {AspectRatio} from "@/components/ui/aspect-ratio";
import {Button} from "@/components/ui/button";
import {SquarePenIcon, XIcon} from "lucide-react";
import {Item, ItemContent, ItemFooter, ItemHeader} from "@/components/ui/item";
import CopyButton from "@/components/CopyButton";
import {Separator} from "@/components/ui/separator";
import {useEffect, useState} from "react";
import WorldCardEditDialog from "@/components/WorldCardEditDialog";
import {Label} from "recharts";
import {
    Select,
    SelectContent,
    SelectGroup,
    SelectItem,
    SelectLabel,
    SelectTrigger,
    SelectValue
} from "@/components/ui/select";

export default function AdminPage() {
    const category: string = DEFAULT_CATEGORY;
    const page: number = DEFAULT_PAGE_NUMBER;
    const pageSize: number = DEFAULT_PAGE_SIZE;
    const {data: worlds, isFetching} = useQuery<WorldPayload[]>({
        queryKey: ['admin', 'worlds', page, pageSize],
        queryFn: async () => fetch(getPageApi(category, page.toString(), pageSize.toString())).then(res => res.json()),
        refetchOnMount: false,
        refetchOnReconnect: true,
    })
    const [editWorld, setEditWorld] = useState<WorldPayload>(null);

    useEffect(() => {
        console.log("editWorld", editWorld);
    }, [editWorld]);

    return (
        <section>
            {/*TODO: 검색창*/}
            {/*일단 월드 리스트부터*/}

            <Separator/>
            <Select>
                <SelectTrigger className="w-[180px]">
                    <SelectValue  placeholder="Select a fruit"/>
                </SelectTrigger>
                <SelectContent arrowPadding={50} className={"w-[1800px]"}>
                    <SelectGroup>
                        <SelectLabel>Fruits</SelectLabel>
                        <SelectItem value="apple">Apple</SelectItem>
                        <SelectItem value="banana">Banana</SelectItem>
                        <SelectItem value="blueberry">Blueberry</SelectItem>
                        <SelectItem value="grapes">Grapes</SelectItem>
                        <SelectItem value="pineapple">Pineapple</SelectItem>
                    </SelectGroup>
                </SelectContent>
            </Select>
            <Table>
                <TableBody>
                    {
                        !worlds && isFetching &&
                        <TableRow>
                            <TableCell colSpan={3} align={"center"}><Spinner className={"size-10"}/></TableCell>
                        </TableRow>
                    }
                    {
                        worlds && worlds.filter(notNull).map((w) => (
                            <TableRow key={w.worldId} className={""}>
                                <TableCell className={"h-50 w-67"}>
                                    <AspectRatio ratio={4 / 3} className={"rounded-xl overflow-hidden"}>
                                        <Image
                                            src={w.metadata.imageUrl} alt={w.metadata.worldName}
                                            decoding={"async"}
                                            loading={"eager"}
                                            unoptimized
                                            aria-hidden={undefined}
                                            draggable={false}
                                            fill
                                        />
                                    </AspectRatio>
                                </TableCell>
                                <TableCell>
                                    <Item className={"gap-0"}>
                                        <ItemHeader className={"text-muted-foreground justify-start"}>
                                            {w.metadata.worldId}
                                            <CopyButton text={w.metadata.worldId}/>
                                        </ItemHeader>
                                        <ItemContent className={"text-3xl font-bold"}>{w.metadata.worldName}</ItemContent>
                                        <ItemFooter className={"text-lg mt-6"}>{w.metadata.authorName}</ItemFooter>
                                    </Item>
                                </TableCell>
                                <TableCell>
                                    <div className={"flex gap-2"}>
                                        <Button className={"btn btn-sm btn-outline"}
                                                onClick={() => setEditWorld(w)}><SquarePenIcon/><span>수정</span></Button>
                                        <Button className={"btn btn-sm btn-outline"}><XIcon/>삭제</Button>
                                    </div>
                                </TableCell>
                                <TableCell>

                                </TableCell>
                            </TableRow>
                        ))
                    }

                </TableBody>
            </Table>

            {
                editWorld &&
                <WorldCardEditDialog worldData={editWorld} onOpenChangeAction={() => setEditWorld(null)}/>
            }
        </section>
    )
}
