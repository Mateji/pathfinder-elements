import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-icon',
    imports: [],
    templateUrl: './icon.html',
    styleUrl: './icon.css'
})
export class Icon {
    @Input({ required: true }) public name!: string;

    @Input() public fill: string | null = null;

    @Input() public size: number = 20;

    @Input() public spritePath: string = 'assets/icons.svg';

    @Input() public ariaLabel: string | null = null;

    public get spriteHref(): string {
        if (!this.name) {
            return '';
        }
        return `${this.spritePath}#${this.name}`;
    }
}
