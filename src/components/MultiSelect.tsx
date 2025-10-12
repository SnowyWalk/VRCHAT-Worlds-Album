"use client"

import {Check, ChevronsUpDown} from "lucide-react"
import {toast} from "sonner"

import {cn} from "@/lib/utils"
import {Button} from "@/components/ui/button"
import {
    Command,
    CommandEmpty,
    CommandGroup,
    CommandInput,
    CommandItem,
    CommandList,
} from "@/components/ui/command"
import {
    Popover,
    PopoverContent,
    PopoverTrigger,
} from "@/components/ui/popover"

export default function MultiSelect({selectedCategoryIdList}: { selectedCategoryIdList: number[] }) {
    const languages = [
        {label: "English", value: "en"},
        {label: "French", value: "fr"},
        {label: "German", value: "de"},
        {label: "Spanish", value: "es"},
        {label: "Portuguese", value: "pt"},
        {label: "Russian", value: "ru"},
        {label: "Japanese", value: "ja"},
        {label: "Korean", value: "ko"},
        {label: "Chinese", value: "zh"},
    ] as const

    const field = {
        value: "en",
    }

    return (
        <section>
            <Popover>
                <PopoverTrigger asChild>
                    <Button
                        variant="outline"
                        role="combobox"
                        className={cn(
                            "w-[200px] justify-between"
                        )}
                    >
                        {field.value
                            ? languages.find(
                                (language) => language.value === field.value
                            )?.label
                            : "Select language"}
                        <ChevronsUpDown className="opacity-50"/>
                    </Button>
                </PopoverTrigger>
                <PopoverContent className="w-[200px] p-0">
                    <Command>
                        {/*<CommandInput*/}
                        {/*    placeholder="카테고리 검색"*/}
                        {/*    className="h-9"*/}
                        {/*/>*/}
                        <CommandList>
                            <CommandEmpty>카테고리 없음</CommandEmpty>
                            <CommandGroup>
                                {languages.map((language) => (
                                    <CommandItem
                                        value={language.label}
                                        key={language.value}
                                        onSelect={() => {
                                        }}
                                    >
                                        {language.label}
                                        <Check
                                            className={cn(
                                                "ml-auto",
                                                language.value === field.value
                                                    ? "opacity-100"
                                                    : "opacity-0"
                                            )}
                                        />
                                    </CommandItem>
                                ))}
                            </CommandGroup>
                        </CommandList>
                    </Command>
                </PopoverContent>
            </Popover>
        </section>
    )
}