import { adminGuard } from "../../common-guards.ts/manager.guard";
import type { LayoutLoad } from "./$types";

export const load: LayoutLoad = ({url}) =>{
    adminGuard(url);
    return{};
}