var dataTable;

$(document).ready(function () {
    loadTableData();
});

function loadTableData() {
    dataTable = $('#tbldata').DataTable({
        "ajax": {
            "url": "/Admin/Company/GetAll"
        },
        "columns": [
            { "data": "name", "width": "15%" },
            { "data": "streetAddress", "width": "15%" },
            { "data": "city", "width": "15%" },
            { "data": "state", "width": "15%" },
            { "data": "phoneNumber", "width": "20%" },
            {
                "data": "isAuthorizedCompany",
                "render": function (data) {
                    if (data) {
                        return `<input type="checkbox" checked disabled>`;

                    }
                    else {
                        return `<input type="checkbox" disabled>`
                    }
                }
        },
            {
                "data": "id",
                "render": function (data) {
                    return `
                   <div class="d-flex justify-content-center gap-2">

    <!-- EDIT -->
    <a href="/Admin/Company/Upsert/${data}" 
       class="btn action-btn edit-btn">

        <i class="fas fa-pen"></i>

    </a>

    <!-- DELETE -->
    <a onclick="Delete('/Admin/Company/Delete/${data}')"
       class="btn action-btn delete-btn">

        <i class="fas fa-trash"></i>

    </a>

</div>`;
                },
                "width": "40%"
            }
        ]
    });
}

function Delete(url) {
    swal({
        title: "Want to delete data?",
        icon: "warning",
        text: "Delete Information",
        buttons: true,
        dangerMode: true
    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({
                url: url,
                type: "DELETE",
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    } else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}