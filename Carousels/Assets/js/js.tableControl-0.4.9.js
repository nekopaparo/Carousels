/* js.tableControl v0.4.9

1. 移除表單樣式 createFormConnection
2. 調整 → 監控 table 被刪除事件
3. table 控制器相關，移置Table
4. 使用者選擇觸發事件控制，移置Select
5. 每個table的各自的使用者選擇控制，移置SelectRows
6. 刪除單選變數，改成SelectRows.data[0]
7. 相關功能統一管理
8. 新增資料鎖定
-----------------------------
9. 移除非必要this，明確指定
10. 排序改成用綁定的th觸發

// 建立控制
const tableConn = TableClient.tableConnection(document.querySelector('table'));

// 設定表格
tableConn.set({
    'columns': [
        '姓名',
        {
            'data': '年紀',
            'title': 'age'
        }
    ],
    'data': [
        {
            '姓名': '貓貓',
            '年紀': 3
        },
        ['兔兔', 2],
        {
            '姓名': '狗狗',
            '年紀': 5
        }
    ],
    "rowCallback": function(row, field, index) {
        // row          = tr
        // field.target  = td;
        // field.data   = 欄位的值
        // index        = row_index
    },
    'pageSize': 2,          // 顯示數量
    'options': {
        'sort': true,       // 排序
        'select': true,     // 單選
        'selects': true     // 多選
    }
});

// 其他功能
tableConn.page;        // 目前頁數 { get; set; }
tableConn.totalPage;   // 總共頁數 { get; }
tableConn.pageSize;    // 顯示數量 { get; set; }
tableConn.reflash();   // 重新整理   
tableConn.clear();     // 清空資料(設定)
tableConn.dispose();   // 釋放控制器
tableConn.locks;       // 取的鎖定資料
tableConn.lock(tr);    // 鎖定，傳入 tr 或 td
tableConn.lockAll();   // 被選擇的全部鎖定(略過已被鎖定)
tableConn.unlock(tr);  // 解除鎖定，傳入 tr 或 td
tableConn.unlockAll(); // 全部解鎖
// 自動排序
dbConnect.options.sort = true
tableConn.order;                 // 排序SQL { get; set; } = 'ColName1, ColName2 DESC'
// 使用者選擇
tableConn.options.select = true  // 限定單選
tableConn.select.Row             // 取得被選擇的資料 {target, data}
tableConn.select.clear()         // 清空選擇
tableConn.select.add(tr)         // 手動加入選擇
tableConn.options.selects = true // 能多選
tableConn.selects.Rows           // 取得被選擇的資料 []
tableConn.selects.clear()        // 清空選擇
tableConn.selects.removeAll()    // 刪除全部選擇
tableConn.selects.add(tr)        // 手動加入選擇
*/

(function (global, factory) {
    "use strict";
    if (typeof module === "object" && typeof module.exports === "object") {
        module.exports = factory();
    }
    else {
        global.TableClient = factory();
    }
})(this, function () {
    // 控制器
    const TABLE = {
        // 控制器
        'connections': [],
        // 控制器索引 key = table
        'connection_index': [],
        // 新增控制器
        'createConnection': function (table) {
            var connection = dbConnection(table);
            TABLE.connections.push(connection);
            TABLE.connection_index.push(table);
            return connection;
        },
        // 刪除控制器
        'removedConnection': function (obj) {
            if (obj instanceof HTMLElement) {
                for (var i = 0; i < TABLE.connection_index.length;) {
                    if (TABLE.connection_index[i] == obj) {
                        var connection = TABLE.connections[i];
                        // 釋放控制器
                        if (connection.dispose) {
                            connection.dispose();
                        }
                        // 移除控制器
                        TABLE.connections.splice(i, 1);
                        // 移除控制器索引
                        TABLE.connection_index.splice(i, 1);
                    }
                    else {
                        ++i;
                    }
                }
            }
            else {
                // 釋放控制器
                if (obj.dispose) {
                    obj.dispose();
                    var i = TABLE.connections.indexOf(obj);
                    if (i >= 0) {
                        // 移除控制器
                        TABLE.connections.splice(i, 1);
                        // 移除控制器索引
                        TABLE.connection_index.splice(i, 1);
                    }
                }
            }
        },
        // 取得控制器
        'getConnection': function(table) {
            /*
            var index = this.indexOf(table);
            if (index >= 0) {
                return this.connections[index];
            }
            else {
                return this.createConnection(table);
            }
            */
            return TABLE.createConnection(table);
        },
        // 取得控制器索引
        'indexOf': function (table) {
            return TABLE.connection_index.indexOf(table);
        },
        // 是否為<table>
        'isTable': function (table) {
            return table instanceof HTMLElement &&
                table.tagName.toLowerCase() == 'table';
        },
        // 刷新
        'refresh': function () {
            TABLE.distinct_table().forEach(function (table) {
                if (!document.contains(table)) {
                    TABLE.removedConnection(table);
                }
            })
        },
        // 取得不重複table
        'distinct_table': function () {
            return TABLE.connection_index.filter(function (table, index) {
                return TABLE.connection_index.indexOf(table) == index;
            });
        }
    }

    // #region 監控 table 被刪除事件
    var observer = new MutationObserver(function (mutations) {
        TABLE.refresh();
    });
    observer.observe(document, {
        childList: true,
        subtree: true,
        target: 'body'
    });
    // #endregion

    // #region 使用者選擇的css樣式
    const selectTarget = 'user-select-row';
    var css = `
table thead th::after {
    color: #01977a;
    content: attr(data-order);
    font-weight: bold;
}
table tbody tr[${selectTarget}] {
    border-radius: 5px;
    background-color: rgb(175, 208, 247);
}
table tbody tr[row-lock] {
    border-radius: 5px;
    background-color: rgb(248, 215, 218);  
}
    `;
    var style = document.querySelector('head').insertBefore(document.createElement('style'), document.querySelector('head style'));
    if (style.styleSheet) {
        style.styleSheet.cssText = css;
    }
    else {
        style.appendChild(document.createTextNode(css));
    }
    // 釋放
    style = undefined;
    css = undefined;
    // #endregion

    // #region 使用者選擇觸發事件控制
    const SELECT = {
        'CtrlKeydown': 0,
        'ShiftKeydown': 0,
        // 拖曳暫存
        'tmp_data': [],
        // 按下按鍵，備註:如果已經按Ctrl(Shift)就不會觸發Shift(Ctrl)
        'keydown': function (e) {
            switch (e.code.toLowerCase()) {
                // 左邊的Ctrl
                case 'controlleft':
                    if (SELECT.ShiftKeydown == 0) {
                        SELECT.CtrlKeydown = 1;
                    }
                    break;
                // 左邊的Shift
                case 'shiftleft':
                    if (SELECT.CtrlKeydown == 0) {
                        SELECT.ShiftKeydown = 1;
                        TABLE.distinct_table().forEach(function (table) {
                            table.style.userSelect = 'none';  // 禁止反白
                        });
                    }
                    break;
            }
        },
        // 放開按鍵
        'keyup': function (e) {
            switch (e.code.toLowerCase()) {
                // 左邊的Ctrl
                case 'controlleft':
                    if (SELECT.CtrlKeydown == 1) {
                        SELECT.CtrlKeydown = 0;
                    }
                    break;
                // 左邊的Shift
                case 'shiftleft':
                    if (SELECT.ShiftKeydown == 1) {
                        SELECT.ShiftKeydown = 0;
                        // 開啟反白
                        TABLE.distinct_table().forEach(function (table) {
                            table.style.userSelect = 'text';
                        });
                        // 清空拖曳暫存
                        SELECT.tmp_data.forEach(function (data) {
                            data.splice(0);
                        });
                    }
                    break;
            }
        },
        // 加入暫存綁定
        'add': function (tmp) {
            if (SELECT.tmp_data.indexOf(tmp) < 0) {
                SELECT.tmp_data.push(tmp);
            }
        },
        // 移除暫存綁定
        'remove': function (tmp) {
            var i = SELECT.tmp_data.indexOf(tmp);
            if (i >= 0) {
                SELECT.tmp_data.splice(i, 1);
            }
        }
    }
    // 加入事件
    document.addEventListener('keydown', SELECT.keydown);
    document.addEventListener('keyup', SELECT.keyup);
    // #endregion

    // 資料綁定 
    const BIND = {
        // 綁定
        'link': function a(bind, target, data) {
            if (Array.isArray(bind)) {
                bind.push({
                    'target': target,
                    'data': data
                });
            }
        },
        // 移除綁定
        'remove': function (bind, target) {
            if (Array.isArray(bind)) {
                for (var i = 0; i < bind.length; ++i) {
                    if (bind[i].target === target || bind[i].data === target) {
                        bind.splice(i, 0);
                        return true;
                    }
                }
            }
            return false;
        },
        // 取得綁定
        'get': function (bind, target) {
            if (target && Array.isArray(bind)) {
                for (var i = 0; i < bind.length; ++i) {
                    if (bind[i].target === target || bind[i].data === target) {
                        return bind[i];
                    }
                }
            }
            return undefined
        }
    }

    function dbConnection(table) {
        var Page = 1;                       // 目前頁數
        var PageSize = undefined;           // 每頁顯示數量
        var fnRowCallback = undefined;      // 自定樣式
        

        var Columns = {
            'data': [],
            'target': undefined,    // thead
            'cache': [],            // 綁定資料
            get thead() {
                if (!this.target || !table.contains(this.target)) {
                    this.target = table.querySelector('thead');
                    if (this.target === null) {
                        this.target = table.appendChild(document.createElement('thead'));
                    }
                }
                return this.target;
            },
            'clear': function () {
                Columns.data = [];
                Columns.cache.splice(0);
                Columns.thead.innerHTML = '';
            }
        };

        var Rows = {
            'data': [],
            'locks': [],            // 鎖定的資料
            'target': undefined,    // tbody
            'cache': [],            // 綁定資料
            get tbody() {
                if (!this.target || !table.contains(this.target)) {
                    this.target = table.querySelector('tbody');
                    if (this.target === null) {
                        this.target = table.appendChild(document.createElement('tbody'));
                    }
                }
                return this.target;
            },
            'clear': function () {
                Rows.data = [];
                Rows.locks.splice(0);
                Rows.cache.splice(0);
                Rows.tbody.innerHTML = '';
            },
            'lock': function (element) {
                var tr = undefined;
                switch (element.tagName.toLowerCase()) {
                    case 'td':
                        tr = element.parentNode;
                        break;
                    case 'tr':
                        tr = element;
                        break;
                }
                var bind = BIND.get(Rows.cache, tr);
                if (bind && bind.data) {
                    if (Rows.locks.indexOf(bind.data) < 0) {
                        Rows.locks.push(bind.data);
                        tr.setAttribute('row-lock', '');
                        return true;
                    }
                }
                return false;
            },
            'lockAll': function () {
                for (var i = 0; i < Select.data.length; ++i) {
                    var bind = BIND.get(Rows.cache, Select.data[i]);
                    if (bind && bind.target && bind.data) {
                        if (Rows.locks.indexOf(bind.data) < 0) {
                            Rows.locks.push(bind.data);
                            bind.target.setAttribute('row-lock', '')
                        }
                    }
                }
            },
            'unlock': function (element) {
                var tr = undefined;
                switch (element.tagName.toLowerCase()) {
                    case 'td':
                        tr = element.parentNode;
                        break;
                    case 'tr':
                        tr = element;
                        break;
                }
                var bind = BIND.get(Rows.cache, tr);
                if (bind && bind.data) {
                    var index = Rows.locks.indexOf(bind.data);
                    if (index >= 0) {
                        Rows.locks.splice(index, 1);
                        tr.removeAttribute('row-lock');
                        return true;
                    }
                }
                return false;
            },
            'unlockAll': function () {
                for (var i = 0; i < Rows.locks.length; ++i) {
                    var bind = BIND.get(Rows.cache, Rows.locks[i]);
                    if (bind && bind.target) {
                        bind.target.removeAttribute('row-lock');
                    }
                }
                Rows.locks.splice(0);
            }
        };

        // 資料控制
        var  DataTable = {
            get columns() {
                if (Columns.cache.length > 0) {
                    return Columns.cache.map(function (bind) {
                        return bind.data;
                    });
                }
                if (Array.isArray(Columns.data)) {
                    return Columns.data.map(function (column) {
                        if (typeof column === 'string') {
                            return column;
                        }
                        if (typeof column === 'object') {
                            if (typeof column.data === 'string') {
                                return column.data;
                            }
                            if (typeof column.title === 'string') {
                                return column.title;
                            }
                        }
                        return undefined;
                    });
                }
                return undefined;
            },
            set columns(value) {
                if (Array.isArray(value)) {
                    Columns.data = value;
                    // 重置排序
                    if (Order.state) {
                        Order.clear();
                    }
                }
            },
            get rows() {
                if (Array.isArray(Rows.data)) {
                    return Rows.data;
                }
                else {
                    return [];
                }
            },
            set rows(value) {
                if (Array.isArray(value)) {
                    Rows.data = value;
                    // 重置排序
                    if (Order.state) {
                        Order.clear();
                    }
                }
            },
            get rowCallback() {
                return fnRowCallback;
            },
            set rowCallback(value) {
                if (typeof value === 'function' || value === undefined) {
                    fnRowCallback = value;
                }
            }
        }

        // 使用者選擇控制
        var  Select = {
            // 被選擇的tr
            'data': [],
            // 啟用狀態 0 關閉、1 單選、2 多選
            'state': 0,
            // 單選
            get single() {
                return this.state == 1;
            },
            set single(value) {
                this.state = value ? 1 : 0;
            },
            // 多選
            get double() {
                return this.state == 2;
            },
            set double(value) {
                this.state = value ? 2 : 0;
            },
            // 拖曳多選暫存
            'tmp': [],
            // 加入選擇
            'add': function (tr) {
                if (tr) {
                    var index = Select.data.indexOf(tr);
                    if (index < 0) {
                        Select.data.push(tr);
                        tr.setAttribute(selectTarget, '');
                        return true;
                    }
                }
                return false;
            },
            // 刪除選擇
            'remove': function (tr) {
                var index = Select.data.indexOf(tr);
                if (index >= 0) {
                    Select.data.splice(index, 1);
                    tr.removeAttribute(selectTarget);
                    return true;
                }
                return false;
            },
            // 刪除選擇(含資料)
            'delete': function (tr) {
                var delete_buffer = null;   // 緩衝
                // 指定刪除
                if (tr) {
                    delete_buffer = [tr];
                }
                // 全部刪除
                else {
                    delete_buffer = [].concat(Select.data); // 指標
                }

                for (var i = 0; i < delete_buffer.length; ++i) {
                    var bind = BIND.get(Rows.cache, delete_buffer[i]);
                    if (bind) {
                        // 刪除row
                        bind.target.remove();
                        // 刪除選擇
                        Select.remove(bind.target);
                        // 刪除綁定資料
                        var index = Rows.data.indexOf(bind.data);
                        if (index >= 0) {
                            Rows.data.splice(index, 1);
                        }
                        // 解除綁定
                        BIND.remove(Rows.cache, bind.target)
                    }
                }
                // 釋放緩衝
                delete_buffer = undefined;
            },
            // 不存在新增，已存在刪除
            'toggle': function (tr) {
                if (Select.isSelect(tr)) {
                    Select.remove(tr);
                }
                else {
                    Select.add(tr);
                }
            },
            // 取消全部選擇
            'clear': function () {
                while (Select.data.length > 0) {
                    Select.remove(Select.data[0]);
                }
            },
            // 清空拖曳暫存
            'clear_tmp': function () {
                while (Select.tmp.length > 0) {
                    Select.remove(Select.tmp[0]);
                    Select.tmp.splice(0, 1);
                }
            },
            // 已被選擇
            'isSelect': function (tr) {
                var index = Select.data.indexOf(tr);
                return index >= 0;
            },
            // 取得資料行
            'tr': function (element) {
                var tr = undefined;
                switch (element.tagName.toLowerCase()) {
                    case 'td':
                        tr = element.parentNode;
                        break;
                    case 'tr':
                        tr = element;
                        break;
                }
                // 檢查是否綁定的資料行
                if (tr) {
                    if (!BIND.get(Rows.cache, tr)) {
                        tr = undefined;
                    }
                }
                return tr;
            },
            // 加入選取前的處理(isToggle 是否觸發已選取時取消選取)
            'push': function (element, isToggle = true) {
                var tr = Select.tr(element);
                if (tr) {
                    if (Select.state > 0) {
                        // 拖拉多選
                        if (Select.double && SELECT.ShiftKeydown) {
                            // 清空拖曳暫存
                            Select.clear_tmp();
                            // 取得目前所有的row
                            var rows = Array.from(Rows.tbody.querySelectorAll('tr'));
                            var start = 0;              // 預設從第一個row開始拖曳
                            var end = rows.indexOf(tr); // 目前被點擊的
                            // 如果已經有被選取的row，從最後一個選取row作為拖曳開始點(參考資料庫樣式)
                            if (Select.data.length > 0) {
                                start = rows.indexOf(Select.data[Select.data.length - 1]);
                            }
                            // 固定從最上面往下加入選取
                            if (start > end) {
                                var tmp = start;
                                start = end;
                                end = tmp;
                            }
                            // 加入選取，已存在不加入，不存在加入暫存(取消選取時刪除用)
                            for (; start <= end; ++start) {
                                if (Select.add(rows[start])) {
                                    Select.tmp.push(rows[start]);
                                }
                            }
                        }
                        // 單筆點擊多選
                        else if (Select.double && SELECT.CtrlKeydown) {
                            if (isToggle) {
                                Select.toggle(tr);
                            }
                            else {
                                Select.add(tr);
                            }
                        }
                        // 單選
                        else {
                            // 清除選擇，重新使用者選取
                            if (Select.data.length > 1 || Select.data[0] !== tr) {
                                Select.clear();
                                Select.add(tr);
                            }
                            else {
                                if (isToggle) {
                                    Select.toggle(tr);
                                }
                                else {
                                    Select.add(tr);
                                }
                            }
                        }
                    }
                }
            },
            // 選擇事件
            'event': function (e) {
                Select.push(e.target);
            }
        }

        // 排序控制
        var  Order = {
            'data': [],
            'clear': function () {
                Order.data.splice(0);
            },
            // 功能啟用狀態
            'state': false,
            // 取的欄位的排序索引
            'indexOf': function indexOrder(column) {
                for (var index = 0; index < Order.data.length; ++index) {
                    if (Order.data[index].column === column) {
                        return index;
                    }
                }
                return -1;
            },
            // 排序
            'sort': function orderSort() {
                var colNames = DataTable.columns;
                if (colNames === undefined) { return; }
                // 指定排序方式，0 不動、1 往後移、-1 往前移
                DataTable.rows.sort(function (a, b) {
                    'use strict'
                    var compare = 0;
                    for (var i = 0; i < Order.data.length; ++i) {
                        var index = colNames.indexOf(Order.data[i].column);
                        if (index < 0) {
                            continue;
                        }
                        var _a = Array.isArray(a) ? a[index] : a[Order.data[i].column];
                        var _b = Array.isArray(b) ? b[index] : b[Order.data[i].column];
                        if (_a > _b) {
                            compare = (Order.data[i].keyword === 'DESC') ? -1 : 1;
                            break;
                        }
                        else if (_a < _b) {
                            compare = (Order.data[i].keyword === 'DESC') ? 1 : -1;
                            break;
                        }
                    }
                    return compare;
                });
                Order.reflash();
            },
            'addIcon': function addIcon(th) {
                var colName = Order.colName(th);
                var index = Order.indexOf(colName);
                if (index >= 0) {
                    switch (Order.data[index].keyword) {
                        case 'ASC': default:
                            th.setAttribute('data-order', '▲');
                            break;
                        case 'DESC':
                            th.setAttribute('data-order', '▼');
                            break;
                    }
                }
            },
            'reflash': function () {
                // 標題
                for (var i = 0; i < Columns.cache.length; ++i) {
                    Order.addIcon(Columns.cache[i].target);
                }
                // 內容
                reflash();
            },
            'colName': function (th) {
                var bind = BIND.get(Columns.cache, th);
                return bind ? bind.data : undefined;
            },
            // 加入事件
            'addEvent': function () {
                Columns.cache.forEach(function (bind) {
                    bind.target.addEventListener('click', Order.add_Event);             // 加入左鍵排序事件
                    bind.target.addEventListener('contextmenu', Order.remove_Event);    // 加入右鍵取消排序
                    Order.addIcon(bind.target);                                         // 加入圖示
                });
            },
            // 刪除事件
            'removeEvent': function () {
                Columns.cache.forEach(function (bind) {
                    bind.target.removeEventListener('click', Order.add_Event);             // 移除左鍵排序事件
                    bind.target.removeEventListener('contextmenu', Order.remove_Event);    // 移除右鍵取消排序
                    bind.target.removeAttribute('data-order');                             // 移除圖示
                });
            },
            // 左鍵排序
            'add_Event': function (e) {
                if (e.target.tagName.toLowerCase() == 'th') {
                    // 參照資料庫排序格式
                    var colName = Order.colName(e.target);
                    if (colName) {
                        var index = Order.indexOf(colName);
                        if (index >= 0) {
                            switch (Order.data[index].keyword) {
                                case 'DESC':
                                    Order.data[index].keyword = 'ASC';
                                    break;
                                case 'ASC': default:
                                    Order.data[index].keyword = 'DESC';
                                    break;
                            }
                        }
                        else {
                            Order.data.push({
                                'column': colName,
                                'keyword': 'ASC'
                            });
                        }
                    }
                    Order.addIcon(e.target);
                    Order.sort();
                }
            },
            // 右鍵移除排序
            'remove_Event': function (e) {
                if (e.target.tagName.toLowerCase() == 'th') {
                    var colName = Order.colName(e.target);
                    var index = Order.indexOf(colName);
                    if (index >= 0) {
                        Order.data.splice(index, 1);
                        e.target.removeAttribute('data-order');
                    }
                    Order.sort();
                    e.preventDefault();
                }
            }
        };

        // 控制器開關
        var  Options = {
            // 排序開關
            get sort() {
                return Order.state;
            },
            set sort(value) {
                if (typeof value === 'boolean' && value !== Order.state) {
                    Order.state = value;
                    if (Order.state) {
                        Order.addEvent();
                        Object.defineProperty(api, 'order', {
                            configurable: true, // 可以使用delete
                            get: function () {
                                return Order.data.map(function (order) {
                                    return order.column + ' ' + order.keyword;
                                }).join(', ');
                            },
                            set: function (value) {
                                if (typeof value === 'string') {
                                    Order.clear();
                                    value.split(',').forEach(function (keyword) {
                                        var keys = keyword.trim().split(' ');
                                        Order.data.push({
                                            'column': keys[0],
                                            'keyword': keys[1] ? keys[1].toUpperCase() : 'ASC'
                                        });
                                    });
                                    if (Order.state) {
                                        Columns.thead.querySelectorAll('tr th').forEach(Order.addIcon);
                                    }
                                    Order.sort();
                                }
                            }
                        });
                    }
                    else {
                        Order.removeEvent();
                        delete api.order;
                    }
                }
            },
            // 選擇開關
            get select() {
                return Select.single;
            },
            set select(value) {
                if (typeof value === 'boolean' && Select.single !== value) {
                    if (value) {
                        // 先關閉多選
                        if (Options.selects) {
                            Options.selects = false;
                        }
                        api.select = {
                            // 手動加入選擇
                            'add': function (tr) {
                                Select.push(Select.tr(tr), false);
                            },
                            // 清除選擇
                            'clear': function () {
                                Select.clear();
                            },
                            get Row() {
                                var row = BIND.get(Rows.cache, Select.data[0]);
                                return row ? row : undefined;
                            }
                        };
                        table.addEventListener('click', Select.event);
                    }
                    else {
                        api.select.clear();
                        delete api.select;
                        table.removeEventListener('click', Select.event);
                    }
                    Select.single = value;
                }
            },
            get selects() {
                return Select.double;
            },
            set selects(value) {
                if (typeof value === 'boolean' && Select.double !== value) {
                    if (value) {
                        // 先關閉單選
                        if (Options.select) {
                            Options.select = false;
                        }
                        api.selects = {
                            // 手動加入選擇
                            'add': function (tr) {
                                Select.push(Select.tr(tr), false);
                            },
                            // 清除選擇
                            'clear': function () {
                                Select.clear();
                            },
                            // 刪除全部選擇
                            'removeAll': function () {
                                Select.delete();
                            },
                            get Rows() {
                                var columns = Columns.cache.map(function (bind) {
                                    return bind.data;
                                })
                                var rows = [];
                                for (var i = 0; i < Select.data.length; ++i) {
                                    var bind = BIND.get(Rows.cache, Select.data[i]);
                                    if (bind) {
                                        // 標頭
                                        bind['columns'] = columns;
                                        // 刪除
                                        bind['remove'] = function () {
                                            // 刪除選擇
                                            Select.delete(this.target);
                                            // 刪除rows綁定
                                            rows.splice(rows.indexOf(this), 1);
                                        }
                                        // 輸出
                                        rows.push(bind);
                                    }
                                }
                                return rows;
                            }
                        }
                        table.addEventListener('click', Select.event);
                        SELECT.add(Select.tmp);
                    }
                    else {
                        api.selects.clear();
                        delete api.selects;
                        table.removeEventListener('click', Select.event);
                        SELECT.remove(Select.tmp);
                    }
                    Select.double = value;
                }
            }
        };

        // 自訂事件
        var  table_event = {
            'reload': new Event('table-reload'),
            'reflash': new Event('table-reflash'),
            'pageChange': new Event('table-page-change'),
            'pageSizeChange': new Event('table-page-size-change'),
            'dispatchEvent': function (e) {
                table.dispatchEvent(e);
            }
        }

        // #region Core
        // 重新建立
        function reload() {
            var thead = Columns.thead;
            Columns.cache.splice(0);
            // 名稱 colName
            if (Array.isArray(Columns.data)) {
                thead.innerHTML = '';
                var thRow = thead.insertRow();
                Columns.data.forEach(function (column) {
                    var th = thRow.appendChild(document.createElement('th'));
                    var data = undefined;
                    switch (typeof column) {
                        case 'string':
                            th.innerText = column;
                            data = column;
                            break;
                        case 'object':
                            if (typeof column.title === 'string') {
                                th.innerText = column.title;
                                data = column.title;
                            }
                            else if (typeof column.data === 'string') {
                                th.innerText = column.data;
                            }
                            if (typeof column.data === 'string') {
                                data = column.data;
                            }
                            break;
                    }
                    BIND.link(Columns.cache, th, data);
                });
            }
            else {
                var ths = Columns.thead.querySelectorAll('tr:last-child th');
                if (ths.length > 0) {
                    ths.forEach(function (th) {
                        var data = th.hasAttribute('data-th') ?
                            th.getAttribute('data-th') : th.innerText;
                        BIND.link(Columns.cache, th, data);
                    });
                }
            }
            Page = 1;
            // 加入排序
            if (Options.sort) {
                Order.reflash();
                Order.addEvent();
            }
            else {
                reflash();
            }
            // 觸發事件
            table_event.dispatchEvent(table_event.reload);
        }
        // 更新內容
        function reflash() {
            var tbody = Rows.tbody;
            tbody.innerHTML = '';
            Rows.cache.splice(0);
            var cols = DataTable.columns;
            for (var row_index = (api.page - 1) * api.pageSize; row_index < api.page * api.pageSize; ++row_index) {
                if (row_index >= DataTable.rows.length) break;
                var row = DataTable.rows[row_index];
                if (typeof row === 'object') {
                    var tbRow = tbody.insertRow();
                    BIND.link(Rows.cache, tbRow, DataTable.rows[row_index]);
                    var cells = [];
                    if (Array.isArray(cols)) {
                        var isA = Array.isArray(row);
                        for (var i = 0; i < cols.length; ++i) {
                            var td = tbRow.insertCell();
                            var value = isA ? row[i] : row[cols[i]];
                            td.innerText = value ? value : '';
                            BIND.link(cells, td, value);
                        }
                    }
                    else {
                        (Array.isArray(row) ? row
                            : Object.keys(row).map(function (key) { return row[key]; })
                        ).forEach(function (value) {
                            var td = tbRow.insertCell();
                            td.innerText = value;
                            BIND.link(cells, td, value);
                        });
                    }

                    // 鎖定資料樣式
                    var lock_index = Rows.locks.indexOf(row);
                    if (lock_index >= 0) {
                        tbRow.setAttribute('row-lock', '');
                    }

                    if (typeof fnRowCallback === 'function') {
                        fnRowCallback(tbRow, cells, row_index);
                    }
                }
            }
            // 觸發事件
            table_event.dispatchEvent(table_event.reflash);
        }
        // #endregion

        // #region API
        // 設定
        function set(init) {
            if (typeof init === 'object') {
                DataTable.columns = init.columns,
                    DataTable.rows = init.data,
                    DataTable.rowCallback = init.rowCallback
                if (typeof init.options === 'object') {
                    Options.sort = init.options.sort;
                    Options.select = init.options.select;
                    Options.selects = init.options.selects;
                }
                var size = parseInt(init.pageSize);
                if (!isNaN(size)) {
                    PageSize = size;
                }
                reload();
            }
        }
        // 重置設定
        function reset() {
            Page = 1;
            PageSize = undefined;
            fnRowCallback = undefined;
            Options.sort = false;
            Options.select = false;
            Options.selects = false;
        }
        // 清空資料
        function clear() {
            Columns.clear();
            Rows.clear();
            Order.clear();
        }
        // 移除控制器
        function dispose() {
            if (api) {
                reset();
                clear();
                table_event = undefined;
                Columns = undefined;
                Rows = undefined;
                DataTable = undefined;
                Options = undefined;
                Select = undefined;
                Object.keys(api).forEach(function (key) {
                    delete api[key];
                });
                TABLE.removedConnection(api);
                api = undefined;
            }
        }
        // #endregion

        var api = {
            'set': set,
            'reload': reload,
            'reflash': reflash,
            'clear': clear,
            'reset': reset,
            'dispose': dispose,
            'options': Options,         // 功能開啟
            get locks() {
                return Rows.locks;
            },
            'lock': Rows.lock,          // 鎖定
            'lockAll': Rows.lockAll,    // 全部鎖定
            'unlock': Rows.unlock,      // 解除鎖定
            'unlockAll': Rows.unlockAll,// 全部解鎖
            // #region 屬性
            get pageSize() {
                if (PageSize === undefined || PageSize < 1) {
                    return DataTable.rows.length;
                }
                else {
                    return PageSize;
                }
            },
            set pageSize(value) {
                value = parseInt(value);
                if (!isNaN(value)) {
                    if (PageSize !== value) {
                        PageSize = value;
                        Page = 1;
                        reflash();
                        table_event.dispatchEvent(table_event.pageSizeChange);
                    }
                }
            },
            get page() {
                return Page;
            },
            set page(value) {
                value = parseInt(value);
                if (!isNaN(value)) {
                    if (value < 1) {
                        Page = this.totalPage;
                    }
                    else if ((value - 1) * this.pageSize >= DataTable.rows.length) {
                        Page = 1;
                    }
                    else {
                        Page = value;
                    }
                    reflash();
                    table_event.dispatchEvent(table_event.pageChange);
                }
            },
            get totalPage() {
                return Math.ceil(DataTable.rows.length / this.pageSize);
            },
            get length() {
                return DataTable.rows.length;
            }
            // #endregion
        };

        return api;
    }

    return Object.freeze({
        'tableConnection': function (table) {
            if (TABLE.isTable(table)) {
                return TABLE.getConnection(table);
            }
            else {
                return undefined;
            }
        }
    });
});