// Type definitions for ag-grid v6.4.2
// Project: http://www.ag-grid.com/
// Definitions by: Niall Crosby <https://github.com/ceolter/>
// Definitions: https://github.com/borisyankov/DefinitelyTyped
export declare class ColumnKeyCreator {
    private existingKeys;
    getUniqueKey(colId: string, colField: string): string;
}
