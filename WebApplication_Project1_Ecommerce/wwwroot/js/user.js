var dataTable;

$(document).ready(function () {
    loadTableData();
});

function loadTableData() {
    dataTable = $('#tbldata').DataTable({
        "ajax": {
            "url": "/Admin/User/GetAll"
        },
        "columns": [
            { "data": "name", "width": "15%" },
            { "data": "email", "width": "15%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "company.name", "width": "15%" },
            { "data": "role", "width": "15%" },
            {
                "data": { id:"id", lockoutEnd: "lockoutEnd" },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();
                    if (lockout > today) {

                        // USER LOCKED → SHOW UNLOCK

                        return `

            <div class="d-flex justify-content-center">

                <a class="btn action-btn unlock-btn"
                   onclick="LockUnlock('${data.id}')">

                    <i class="fas fa-lock-open"></i>

                </a>

            </div>
            `;
                    }
                    else {

                        // USER UNLOCKED → SHOW LOCK

                        return `

            <div class="d-flex justify-content-center">

                <a class="btn action-btn lock-btn"
                   onclick="LockUnlock('${data.id}')">

                    <i class="fas fa-lock"></i>

                </a>

            </div>
            `;
                    }
                },
              
            }
        ]
    });
}

function LockUnlock(id) {
    $.ajax({
        url: "/Admin/User/LockUnlock",
        type : "POST",
        data: JSON.stringify(id),
        contentType: "application/json",
        success : function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
            else {
                toastr.error(data.message);
                dataTable.ajax.reload();
            }
        }
    })
}