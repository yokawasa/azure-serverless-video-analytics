<div class="row">
    <div id="video-uploader-manual-trigger"></div>
    <script>
        var videoUploader = new qq.azure.FineUploader({
            element: document.getElementById('video-uploader-manual-trigger'),
            template: 'qq-template-video-trigger',
            request: {
                endpoint: "<?php echo $cfg['video_upload_blob_endpoint']; ?>"
            },
            signature: {
                endpoint: '/api/signature.php'
            },
            uploadSuccess: {
                endpoint: '/api/video-upload-success.php',
                params: { type: 'video' }
            },
            retry: {
                enableAuto: true
            },
            deleteFile: {
                enabled: false
            },
            blobProperties:{
                name: 'filename'   /*default: uuid*/
            },
            thumbnails: {
                placeholders: {
                    waitingPath: '/azurefineuploader/waiting-generic.png',
                    notAvailablePath: '/azurefineuploader/not_available-generic.png'
                }
            },
            autoUpload: false,
            debug: true
        });
        qq(document.getElementById("video-trigger-upload")).attach("click", function() {
            videoUploader.uploadStoredFiles();
        });
    </script>
</div>
