"use client";

import {Dialog, DialogClose, DialogContent, DialogHeader, DialogTitle} from "@/components/ui/dialog";
import WorldCard from "@/components/WorldCard";
import {WorldPayload} from "@/schemas/WorldPayload";
import {Item, ItemContent, ItemDescription, ItemHeader} from "@/components/ui/item";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import MultiSelect from "@/components/MultiSelect";
import {useLangStore} from "@/stores/lang-store";

export default function WorldCardEditDialog({worldData, onOpenChangeAction}: {
    worldData: WorldPayload & {},
    onOpenChangeAction: (open: boolean) => void
}) {

    const lang = useLangStore(e => e.lang);

    return (
        <section>
            <Dialog defaultOpen onOpenChange={onOpenChangeAction}>
                <DialogContent>
                    <DialogHeader><DialogTitle>{worldData.metadata.worldName} by {worldData.metadata.authorName}</DialogTitle></DialogHeader>

                    <Item>
                        <ItemHeader><Label htmlFor={"id_description"} className={"text-base"}>월드 소개</Label></ItemHeader>
                        <ItemContent><Input id="id_description"/></ItemContent>
                    </Item>

                    <Item>
                        <ItemHeader><Label htmlFor={"id_category"} className={"text-base"}>카테고리</Label></ItemHeader>
                        <ItemContent>
                            <MultiSelect selectedCategoryIdList={[]}/>
                        </ItemContent>
                    </Item>

                    <div
                        className={"fixed h-[100dvh] w-[15dvw] -right-4 top-[50%] translate-x-[100%] translate-y-[-50%] content-center "}>
                        <WorldCard
                            worldId={worldData.worldId}
                            worldName={worldData.metadata.worldName}
                            authorId={worldData.metadata.authorId}
                            authorName={worldData.metadata.authorName}
                            imageUrl={worldData.metadata.imageUrl}
                            capacity={worldData.metadata.capacity}
                            visits={worldData.metadata.visits}
                            favorites={worldData.metadata.favorites}
                            heat={worldData.metadata.heat}
                            popularity={worldData.metadata.popularity}
                            tags={worldData.metadata.tags}
                            imageList={worldData.imageList}
                            category={worldData.category}
                            description={worldData.description}
                            dataCreatedAt={worldData.dataCreatedAt}
                            lastFolderModifiedAt={worldData.lastFolderModifiedAt}
                            onClickThumbnailAction={() => false}
                        />
                    </div>
                </DialogContent>

            </Dialog>

        </section>
    )

}