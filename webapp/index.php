<?php
require_once dirname(__FILE__) . '/lib/cosmos-meta.php';
require_once dirname(__FILE__) . '/lib/utils.php';
require_once dirname(__FILE__) . '/lib/config.php';

$cfg = GET_CONFIG();
$db  = new CosmosMetadata();
$contents_arr = $db->getList();
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="description" content="Video Cognitive Analytics Sample UI">
    <meta name="viewport" content="width=device-width, initial-scale=1">

    <?php include_once(dirname(__FILE__) . '/components/uploader_head.php'); ?> 

    <title>Video Cognitive Analytics</title>
    <!-- Bootstrap core CSS -->
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css">
    <!-- Custom styles for this template -->
    <link href="/css/design-index.css" rel="stylesheet">
</head>
<body>

<nav class="navbar navbar-inverse navbar-fixed-top">
<div class="container">
    <ul class="nav navbar-nav">
    <li class="active"><a href="">Video Cognitive Analytics</a></li>
    </ul>
</div>
</nav>

<div class="container"> <!-- container start -->
<h2>Video Uploader</h2>
<?php include_once(dirname(__FILE__) . '/components/uploader_body.php'); ?> 
<p></p>
<h2>Video Contents</h2>
<?php include_once(dirname(__FILE__) . '/components/content_list.php'); ?> 
</div><!-- /.container -->

<!-- Placed at the end of the document so the pages load faster -->
<script src="https://ajax.googleapis.com/ajax/libs/jquery/3.2.1/jquery.min.js"></script>
<script src="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js"></script>
</body>
</html>
