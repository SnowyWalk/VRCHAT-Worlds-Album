import {redirect} from "next/navigation";
import {getDefaultPageUrl} from "@/utils/url-util";

export default function HomePage() {
    redirect(getDefaultPageUrl());
}
