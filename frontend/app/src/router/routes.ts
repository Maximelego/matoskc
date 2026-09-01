import { Component } from "vue";

export type Route = {
    name: string;
    path: string;
    component: Component;
}



export const Routes = []