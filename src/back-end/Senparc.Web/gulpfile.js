/// <binding AfterBuild='default' Clean='clean' />
/*
This file is the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');
var del = require('del');

var ts = require("gulp-typescript");
var tsProject = ts.createProject("tsconfig.json");


var paths = {
    scripts: ['scripts/**/*.js', 'scripts/**/*.ts', 'scripts/**/*.map']
};

gulp.task('clean', function () {
    return del(['wwwroot/scripts/**/*']);
});

gulp.task('default', function () {
    //gulp.src(paths.scripts).pipe(gulp.dest('wwwroot/scripts'))

    return tsProject.src()
        .pipe(tsProject())
        .js.pipe(gulp.dest("wwwroot/scripts"));

});

var merge = require('merge-stream');
// Old bower behavior would be "*" in before and "" in after but you don't want that much.
var webpackages = {
    "requirejs": { "bin/*": "bin/" }
    // ...
};

//gulp.task("dist_lib", function () {
//    var streams = [];
//    for (var package in webpackages)
//        for (var item in webpackages[package])
//            streams.push(gulp.src("node_modules/" + package + "/" + item)
//                .pipe(gulp.dest("lib/" + package + "/" + webpackage[package][item])));
//});