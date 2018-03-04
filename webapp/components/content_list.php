<div class="row">
<div id="mediaContainer">
<?php
    $content_index = 1;
    foreach ($contents_arr as $content) {
        $content_id = $content["id"];
        if (!array_has_value_of("name", $content) && !array_has_value_of("asset_name", $content)) {
            continue;
        }
        $name = (array_has_value_of("name", $content)) ? $content["name"] : $content["asset_name"];
        $thumbnail_url = $content["thumbnail_url"];
        $s = sprintf("<div class=\"col-md-4\" style=\"text-align:center\"><a href=\"video/%s\"><img src=\"%s\" height=200><br><div style=\"height:100px\"><b>%s</b></a></div></div>",
                    $content_id,
                    $thumbnail_url,
                    $name);
        echo $s;
        $content_index++;
    }
?>
</div><!-- //mediaContainer -->
</div><!-- //row -->
