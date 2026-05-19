var dataTable;

$(document).ready(function () {
    loadTableData();
});

function loadTableData() {
    dataTable = $('#tbldata2').DataTable({
        "ajax": {
            "url": "/Admin/coverType/GetAll"
        },
        "columns": [
            { "data": "name", "width": "70%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                    <div class="d-flex justify-content-center gap-2">

    <!-- EDIT -->
    <a href="/Admin/coverType/Upsert/${data}" 
       class="btn action-btn edit-btn">

        <i class="fas fa-pen"></i>

    </a>

    <!-- DELETE -->
    <a onclick="Delete('/Admin/coverType/Delete/${data}')"
       class="btn action-btn delete-btn">

        <i class="fas fa-trash"></i>

    </a>

</div>`;
                },
                "width": "30%"
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