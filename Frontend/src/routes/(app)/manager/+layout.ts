import { adminGuard } from "../../common-guards.ts/manager.guard";
import type { LayoutLoad } from "./$types";
export const ssr = false;
export const prerender = false;
export const load: LayoutLoad = ({url}) =>{
    adminGuard(url);
    return{};
}